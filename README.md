# PSA

This project is a Unity-based application, that has a visual scene that adapts based on the users detected emotion. The scene is set in a theatre room, and audience members numbers and behaviours changed based on emotions detected via auditory and visual modes. The goal of this project is to:

-  See if the multimodal emotion detection system is effective for real-time anxiety detection, and  
 -  Observe the effectiveness of real-time adaptive scenes on a users anxiety level.

## Installation
To get started with the project, follow the steps outlined below.

### Prerequisites
 - Unity 6000.1.15f1: Download and install this version from the [Unity Editor Archive](https://unity.com/releases/editor/archive).
 - Working camera and microphone, these can be your machines internal devices (do not need external mic or cam).

### Steps to Install
- Clone this repository to your local machine.
- Launch Unity 6000.1.15f1, then open the project via Unity Hub, and add it.
- Open the Scene named "test" by navigating the project directory under "Assets", then dragging the scene into the Unity hierarchy.
- Press the play button to start running the project.

## Deviations from the Project Plan
The original plan included the usage of a React + JavaScript based web interface as the main application, with the usage of Three.js for 3D graphics. However, during planning of the project, the team found limitations with this approach and decided on Unity as the better option moving forward. This was due to a number of factors, including the adoption of the emotion recognition models modes of data collection (camera and microphone integration), and the free 3D assets available for use.

Additionally, there were limitations on the proposed GFMamba model. Instead a combination of an audio-based anxiety detector and the dima806/facial_emotions_image_detection model, an open-sourced, fine-tuned Convolutional Neural Network (CNN) available on the Hugging Face Hub, was used.

## Attribution

This project makes use of the following third-party assets from the Unity Asset Store.
All assets are used in accordance with their respective license terms and remain the property of their original creators.

Gwangju Theater

URL: https://assetstore.unity.com/packages/3d/environments/gwangju-theater-282533
Publisher: ONSHOP
Description: A detailed 3D recreation of a Korean-style theater environment, used as part of the project’s interior setting.

City People – Free Samples

URL: https://assetstore.unity.com/packages/3d/characters/city-people-free-samples-260446
Publisher: 3DPEOPLE
Description: A collection of animated 3D human models used to populate scenes with realistic background characters.

