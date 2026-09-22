# AR Hide and Seek 🎮

An immersive, hands-free 2-player spatial Augmented Reality game that brings screen-bound camouflage gameplay into real-world physical environments, from the real world game "Meccha Chameleon". Powered by a custom wearable sensor glove, FPGA-accelerated AI gesture classification, and multi-device AR spatial synchronization.

---

### 🕹️ How It Works

1. **Hider Phase:** The Hider wears the hardware glove to spawn a 3D Meccha model onto detected physical surfaces, fine-tunes its 6-DOF placement, paints custom camouflage textures, and locks its spatial coordinates in real time.
2. **Seeker Phase:** The Seeker uses a phone-mounted visualizer to search the physical space. Aiming with a gesture-controlled virtual weapon, the Seeker fires server-validated raycast shots to locate and destroy the hidden target before time runs out.

---

### 🏗️ System Architecture

* **Physical Layer:** Custom fabric glove equipped with 5x flex sensors and an MPU6050 IMU wired to a FireBeetle ESP32 node for real-time sensor data aggregation.
* **AI & Acceleration Layer:** Ultra96-V2 FPGA board running an accelerated ML inference model via DPU to classify motion streams into low-latency action triggers.
* **Networking Layer:** Encrypted TLS socket connections to an AWS EC2 MQTT broker managing master-slave state synchronization and shared spatial anchor delivery.
* **Application Layer:** Unity 2022 LTS (URP) mobile visualizer utilizing ARCore surface tracking and optical hand tracking (XR Hands / MediaPipe) for spatial rendering.

---

### 🛠️ Tech Stack

**Hardware:** Ultra96-V2 FPGA • FireBeetle ESP32-E • MPU6050 IMU • Conductive Flex Sensors  
**Software :** Unity 2022.3 LTS • Google ARCore • MediaPipe / XR Hands • AR Foundation • Vuforia Engine • C#   
**Backend & Protocols:** AWS EC2 • MQTT over TLS • UART (115200 Baud)  

---

### 👥 Team & Governance

Developed as a senior capstone engineering project at the **National University of Singapore (NUS)** under a specialized domain-ownership model:

* **Hardware Lead:** Glove electronics, circuitry, and sensor integration
* **Software/Hardware AI Lead:** Ultra96 FPGA model acceleration and DPU deployment
* **Communications Lead:** TLS/MQTT networking and state synchronization
* **Software Visualizer Lead:** Unity AR graphics, spatial anchors, and state management
