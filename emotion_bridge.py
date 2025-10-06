import argparse
import json
import os
import sys
import time
from datetime import datetime

import cv2

# Ensure we can import emotion_detector from this folder
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
if SCRIPT_DIR not in sys.path:
    sys.path.insert(0, SCRIPT_DIR)

from emotion_detector import EmotionDetector  # noqa: E402


NEGATIVE = {
    "angry",
    "anger",
    "fear",
    "sad",
    "disgust",
    "contempt",
    "worried",
    "afraid",
    "fearful",
}
POSITIVE = {"happy", "happiness", "joy", "joyful", "excited", "content"}
NEUTRAL = {"neutral", "calm"}
SURPRISE = {"surprise", "surprised"}


def coarse_anxiety_1_5(emotion: str, confidence: float) -> int:
    e = (emotion or "").lower()
    if e in NEGATIVE:
        # map confidence 0..1 into {3,4,5}
        val = 3 + round(max(0.0, min(1.0, confidence)) * 2)
        return int(max(3, min(5, val)))
    if e in POSITIVE:
        return 1 if confidence >= 0.6 else 2
    if e in NEUTRAL:
        return 2 if confidence >= 0.6 else 3
    if e in SURPRISE:
        return 3
    # unknown/other
    return 3


def coarse_anxiety_1_10(emotion: str, confidence: float) -> int:
    e = (emotion or "").lower()
    # Neutral should map to 3..5 on 1-10 scale
    if e in NEUTRAL:
        if confidence >= 0.8:
            return 5
        if confidence >= 0.5:
            return 4
        return 3
    # Otherwise, derive from legacy 1-5 mapping for continuity
    s5 = coarse_anxiety_1_5(emotion, confidence)
    return int(max(1, min(10, s5 * 2)))


def atomic_write_json(path: str, obj: dict):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    tmp = f"{path}.tmp"
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=True)
    os.replace(tmp, path)


def main():
    ap = argparse.ArgumentParser(
        description="Run face emotion -> coarse anxiety (1..5) and write JSON for Unity"
    )
    ap.add_argument("--camera", type=int, default=0, help="Webcam index")
    ap.add_argument(
        "--json",
        type=str,
        default=os.path.join(SCRIPT_DIR, "RuntimeData", "face_emotion.json"),
    )
    ap.add_argument("--fps", type=float, default=2.0, help="Update rate")
    ap.add_argument("--nogui", action="store_true", help="Do not open any windows")
    args = ap.parse_args()

    det = EmotionDetector(use_vllm=False, enable_valence_arousal=False)

    cap = cv2.VideoCapture(args.camera)
    if not cap.isOpened():
        print(f"[bridge] Could not open camera index {args.camera}", file=sys.stderr)
        return 1

    last_emit = 0.0
    interval = 1.0 / max(0.1, args.fps)

    try:
        while True:
            ok, frame = cap.read()
            if not ok:
                print("[bridge] Failed to read frame", file=sys.stderr)
                time.sleep(0.2)
                continue

            # Run emotion detection without showing window
            _, results = det.detect_emotions(frame)

            best = None
            if results:
                # choose by highest confidence
                best = max(results, key=lambda r: r.get("confidence", 0.0))

            now = time.time()
            if now - last_emit >= interval:
                if best:
                    raw_emotion = best.get("emotion", "").lower()
                    # normalize via detector mapping if possible
                    canonical = det.emotion_mapping.get(raw_emotion, raw_emotion)
                    conf = float(best.get("confidence", 0.0))
                    score = coarse_anxiety_1_5(canonical, conf)
                else:
                    canonical = "none"
                    conf = 0.0
                    score = 3

                # Map to both legacy 1-5 and preferred 1-10 (neutral -> 3..5)
                score5 = int(score)
                score10 = coarse_anxiety_1_10(canonical, conf)

                payload = {
                    "timestamp": now,
                    "emotion": canonical,
                    "confidence": conf,
                    "faces_detected": len(results) if results is not None else 0,
                    "coarse_anxiety_1_5": score5,
                    "coarse_anxiety_1_10": score10,
                }
                atomic_write_json(args.json, payload)
                last_emit = now

            # keep capture loop in sync
            time.sleep(0.01)

    except KeyboardInterrupt:
        pass
    finally:
        cap.release()
        try:
            cv2.destroyAllWindows()
        except Exception:
            pass

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
