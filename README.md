# PSA

This project is a Unity-based application, that has a visual scene that adapts based on the users detected emotion. The scene is set in a theatre room, and audience members numbers and behaviours changed based on emotions detected via auditory and visual modes. The goal of this project is to:

- See if the multimodal emotion detection system is effective for real-time anxiety detection, and  
- Observe the effectiveness of real-time adaptive scenes on a users anxiety level.

## Installation

To get started with the project, follow the steps outlined below.

### Prerequisites

- Unity 6000.1.15f1: Download and install this version from the [Unity Editor Archive](https://unity.com/releases/editor/archive).
- Python 3.8 or higher: Required for running the emotion detection system.
- Working camera and microphone, these can be your machines internal devices (do not need external mic or cam).

### Steps to Install

- Clone this repository to your local machine.
- Install the required Python packages for the emotion detection system by running:
  ```bash
  pip install -r requirements.txt
  ```
  Note: The PyTorch installation defaults to CPU. If you need CUDA support for GPU acceleration, please install the appropriate PyTorch version from [pytorch.org](https://pytorch.org).
- Launch Unity 6000.1.15f1, then open the project via Unity Hub, and add it.

### Testing the Scene Adaptation

There are two scenes available for testing different aspects of the system:

#### Test Scene (Simulated PSA)
- Open the Scene named "test" by navigating the project directory under "Assets", then dragging the scene into the Unity hierarchy.
- Press the play button to start running the project.
- In this scene, you can test how the scene adapts dynamically to simulated user PSA by pressing keyboard keys **1 through 0** to simulate PSA scores from **1 to 10**.

#### Sample Scene (Real-time Multimodal PSA Detection)
- To test the multimodal PSA detection engine with real camera and microphone input, open the "SampleScene" instead by navigating to "Assets/Scenes" and dragging it into the Unity hierarchy.
- Press the play button to start running the project.
- The detected PSA scores will be displayed on the screen in real-time.
- A complete log of the detection session will be saved to `RuntimeData/psa_log.ndjson` in the project root directory

## Deviations from the Project Plan

The original plan included the usage of a React + JavaScript based web interface as the main application, with the usage of Three.js for 3D graphics. However, during planning of the project, the team found limitations with this approach and decided on Unity as the better option moving forward. This was due to a number of factors, including the adoption of the emotion recognition models modes of data collection (camera and microphone integration), and the free 3D assets available for use.

Additionally, there were limitations on the proposed GFMamba model. Instead a combination of an audio-based anxiety detector and the dima806/facial_emotions_image_detection model, an open-sourced, fine-tuned Convolutional Neural Network (CNN) available on the Hugging Face Hub, was used.

## Contributors

- [Stephen Fang](https://github.com/shinramenisbae)
- [Nicholas Lianto](https://github.com/nlia656)
- [Thisuka Matara Arachchige](https://github.com/ThisukaM)
- [Hamish Patel](https://github.com/HamishPatel)
- [Ronak Patel](https://github.com/Ronak1605)
- [Zion Suh](https://github.com/zsuh3)
- [Yuanyuan Zhang](https://github.com/Hapy-Ismart)

## Attribution

This project makes use of the following third-party assets from the Unity Asset Store.
All assets are used in accordance with their respective license terms and remain the property of their original creators.

Gwangju Theater

URL: <https://assetstore.unity.com/packages/3d/environments/gwangju-theater-282533>
Publisher: ONSHOP
Description: A detailed 3D recreation of a Korean-style theater environment, used as part of the project’s interior setting.

City People – Free Samples

URL: <https://assetstore.unity.com/packages/3d/characters/city-people-free-samples-260446>
Publisher: 3DPEOPLE
Description: A collection of animated 3D human models used to populate scenes with realistic background characters.
