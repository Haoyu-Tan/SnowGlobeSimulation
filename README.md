# SnowGlobeSimulation

## Introduction

## User Instruction
### Executable File
For Windows, there is an executable file named **Snow Globe Simulation.exe** in the ./Snow_Globe_EXE folder. Double click on this .exe file to run the simulation

### Commands
#### Keyboard Interaction
* ‘w’, ’s’  - Move the camera to the front or back
*  ‘a’, ’d’ - Move the camera to the left or right
* Arrow up/ Arrow down - Rotate the camera to up or down
* Arrow left/ Arrow right - Rotate the camera to right or left
#### Mouse Interaction
* When mouse is on the object which could be interacted, there is an indicator on top of it
* You can use the mouse to click and drag the snow globe on the table.
* When you click the green button, the light and music will turn on and off based on current status.
* When you click the red button, the snow globe will change status.


## Implementation
### Step 1 - Water Particles
Based on our observation, the snow globes are usually filled with fluids. In our implementation, we first filled our snow globe with water using SPH water technique. We also observed that the fluid inside the fully filled snow globe is not “visible” from outside so we do not render the water particles in our scene. After tunning the parameters, we have about 250 water particles in the snow globe.

### Step 2 - Snow Particles
In this step, we implement the snow particles based on the water particles we have. We observed that in some snow globes with swirling water, the snow particles inside the water have similar motion as the water.  From this feature, we assumed that the trajectory of a snow particle could be majorly affected by three different facts: the motion of the waters nearby, the motion of the snow particle neighbors, and other forces (e.g., gravity).

To calculate the influence of snow particle trajectory which comes from motion of the waters nearby, we proposed a method similar to the double density relaxation step of the 2005 paper. In our implementation, only water particles inside a chosen range of a snow particle can affect the motion of the snow particle. The equation we used is shown below.

We updated the position of snow particles using the updated velocity and we implemented the double density relaxation based on our snow particles. This will allow the snow particles to achieve some fluid-like features. The neighboring snow particles now could have similar behaviors and they will not be too close to each other.

Moreover, the snow particles can not  bump into the models in the middle of the snow globe. In order to archive this, we used the ray-cast method to detect whether the snow particles will enter the model. When a ray hits the model in the middle, we will set the snow particle to the point where the ray and surface of the model intersects, and we will further move the particle away from the model surface using a small vector in the direction of the normal of the model surface.


### Step 3 - Spatial Data Structure
After the second step, we generated about 500 snow particles based on about 250 water particles. However, this amount of particles largely drag down the overall performance. In order to improve the performance, we implemented the spatial data structure which is similar to the data structure described in the 2005 paper. We first divided the space into cubes which have the length, width and height equals to the radius of each particle. After that, we assign each particle to the cubs. We test collisions between the current particle and the particles in the current cube and the neighboring cubes. We implemented the data structure for water-water pairs, water-snow pairs and snow-snow pairs. After the implementation of the data structure, our framerate is now becoming stable around 20 fps.

### Step 4 - Functions
For the last step, we implemented functions like shaking the globe and changing the mode of the globe to improve the user experience.
