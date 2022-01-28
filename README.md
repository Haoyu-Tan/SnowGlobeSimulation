# SnowGlobeSimulation

## Introduction
For this final project, we implemented the snow globe simulation majorly based on the technique of SPH Water and Spatial Data Structure. Our implementation of snow globe contains two layers. The first layer contains the water particles and the second layer contains the snow particles. The first layer is not rendered, however, it will impact the motion of snow particles in the second layer.

## User Instruction
### Executable File
For Windows, there is an executable file named **Snow Globe Simulation.exe** in the ./Snow_Globe_EXE folder. Double click on this .exe file to run the simulation

### Commands
#### Keyboard Interaction
* ‘w’, ’s’  - Move the camera to the front or back
*  ‘a’, ’d’ - Move the camera to the left or right
* Arrow up/ Arrow down - Rotate the camera to up or down (X-axis)
* Arrow left/ Arrow right - Rotate the camera to left or right (Y-axis)
#### Mouse Interaction
* When mouse is on the object which could be interacted, there is an indicator on top of it
* You can use the mouse to click and drag the snow globe on the table.
* When you click the green button, the light and music will turn on and off based on current status.
* When you click the red button, the snow globe will change status.

## Demo

Link: https://youtu.be/4-0VpZWncjQ

[![67795320827db5b9e0b8aa37b4785f4](https://user-images.githubusercontent.com/35856355/148736614-46daf189-1bab-4f2c-9db0-be879a2b29b6.png)](https://youtu.be/4-0VpZWncjQ)

## Implementation
### Step 1 - Water Particles
Based on our observation, the snow globes are usually filled with fluids. In our implementation, we first filled our snow globe with water using SPH water technique. We also observed that the fluid inside the fully filled snow globe is not “visible” from outside so we do not render the water particles in our scene. After tunning the parameters, we have about 250 water particles in the snow globe.

### Step 2 - Snow Particles
In this step, we added the snow particles on the water particles we have. We observed that in some snow globes with swirling water, the snow particles inside the water have similar motion as the water. Based on this feature, we assumed that the trajectory of a snow particle could be majorly affected by three different facts: the motion of the waters nearby, the motion of the snow particle neighbors, and other forces (e.g., gravity).

To calculate the influence of snow particle trajectory which comes from motion of the waters nearby, we proposed a method similar to the double density relaxation step of the 2005 paper[1]. In our implementation, only water particles inside a chosen range of a snow particle can affect the motion of the snow particle. In order to generate a good simulation, we tried 3 different versions of equations here.

**Version 1**

![image](https://user-images.githubusercontent.com/81786534/146889243-32845342-3fab-47d7-80cb-b3f8f5074112.png)


The basic idea is to calculate the velocity change based on the neighbor water particles of snow particles index i. Here, NWPI stands for neighbor water particle indexes of snow particle i. For each pair of snow particle i and water particle j, we use the distance factor, calculated from the distance between them. We multiplied the distance factor on the velocity of water particle j to calculate the effect of water particle j on snow particle i. We took the average of these effects calculated, multiplied it with coefficient K, and used it as the overall effect of neighbor water particles on snow particle i. 

This method is on the right track. Nonetheless, as the effect is calculated based on the velocity of the water particles, the snow particles could be accelerated too much. This is why we came up with Equation Version 2.

**Version 2**

![image](https://user-images.githubusercontent.com/81786534/146889069-deb1cc6b-9d0c-45f2-9e81-6862a3d14551.png)

In this version, we used the difference of velocity between the water particle j and snow particle i instead of the velocity of water particle j. The idea behind this is: the snow particle, under the influence of water, will finally have the same velocity as the water surrounding it. This method solves the overspeed problem in Version 1. However, the simulation is still not good because the performance of each snow particle still lacks characteristics. To improve the simulation, we came up with Equation Version 3. 

**Version 3**

![image](https://user-images.githubusercontent.com/81786534/146889119-3148256c-09dd-4e0b-8867-43b610a2665a.png)

In this version, we used the sum of water effects instead of the average of water effects.We chose a smaller coefficient K to balance the result.  Using the sum of water effects allows the result to be divergent for each snow particle. This is also the final version of the equation we implemented in our project.


We updated the position of snow particles using the updated velocity and we implemented the double density relaxation among our snow particles. This will allow the snow particles to achieve some fluid-like features. The neighboring snow particles now could have similar behaviors and they will not be too close to each other.

Moreover, the snow particles can not bump into the models in the middle of the snow globe. In order to achieve this goal, we did the ray-cast to detect whether the snow particles will enter the model. When a ray hits the model in the middle, we will set the snow particle to the point where the ray and surface of the model intersects, and we will further move the particle away from the model surface using a small vector in the direction of the normal of the model surface.


### Step 3 - Spatial Data Structure
After the second step, we generated about 500 snow particles. We had about 750 particles totally in the globe. However, this amount of particles largely draged down the overall performance. In order to improve the performance, we implemented the spatial hashing data structure described in paper[2], similar to 2005 paper. We first divided the space into cubes which have the length, width and height equals to the radius of each particle. After that, we assign each particle to the cubes. We test collisions between the current particle and the particles in the current cube and the neighboring cubes. We implemented the data structure for all water-water pairs, water-snow pairs and snow-snow pairs. After implementing this data structure, we found our framerate was stable at around 20 fps and the simulation looked more stable. We also noticed that by using this method, the frame rate did not drop too much with the number of particles increased.

### Step 4 - Functions
For the last step, we implemented functions like shaking the globe and changing the mode of the globe to improve the user experience.

## Comparison with State of the Art

The only work we found related to the simulation inside a globe is a project proposed by John Turner[3]. Similar to our idea, Turner implemented the particle-based snowflake and fluids. However, different from our project which implemented SPH water, Turner implemented the fluid using the 3D grid-based Eulerian fluid solver. We chose the particle based method because it is simple, straight forward, and guarantees mass conservation. For snow globe interaction, Turner added mouse click to apply drag force to the walls of the globe without moving the globe. In other words, the mouse click added drag force to the Eulerian bounds of the globe. Different from Turner’s implementation, we directly applied force onto the snow particles when the snow globe was shaken. For collision detection, Turner only checked the collision between snowflakes and the globe using the spherical collider of the globe. Besides handling the collision between particles and the spherical globe, we also handle collision between snow particles and models in the globes. In conclusion, the method we used is more straightforward, easy to understand and follows the physical rules. However, the number of particles we could have in the snow globe was significantly smaller than Turner’s simulation. 


## Limitations and Future Works
There are several limitations in our simulation.

One limitation is although we added the spatial data structure to improve the performance of simulation, the amount of snow particles we have is still much smaller than that in reality. We found most of the time was spent on this simulation was to find the neighbors of each particle and calculate the distance between them. We did this process every frame several times as described in the implementation section. For future work, we either need to think out a way to further reduce the number of this process or look for more advanced data structures to allow us to have more number of both water and snow particles while keeping a reasonable frame rate.

Another limitation we notice is that snow particles are easily distributed off the center. The reason behind this phenomenon might be the water particles in the center push their neighbors out and these impacted water particles then further pushed the snow particles. For future works, we can try and add a small force to push the snow particles toward the center.

Besides the above limitation we noticed, we also received some feedback that we think worthy to have a try in the future.

* First, currently our snow particles do not stay long and accumulate on the surface of models. In the future, we can add some frictions on snow particles so they are able to stay on the surfaces of models and naturally pile together.

* Second, we only add 2D interaction on our snow globe for shaking. We can extend the interaction to 3D so users are able to freely interact and shake the snow globe in any dimension.

## References
### Reference Paper and Project:

1. Simon Clavet, Philippe Beaudoin, and Pierre Poulin. 2005. Particle-based viscoelastic water simulation. In Proceedings of the 2005 ACM SIGGRAPH/Eurographics symposium on Computer animation (SCA '05). Association for Computing Machinery, New York, NY, USA, 219–228. 
2. Teschner M., Heidelberger B., Mueller M., Pomeranets D., Gross M.: Optimized spatial hashing for collision detection of deformable objects. In Vision, Modeling, and Visualization(2003), 47-54
3. Snow Globe 2 (3D Fluid and Particle now in a Globe)
Link: http://johnmturner.com/cvpages/sim/simProj9.html

### Prefabs, Textures and Music
1. https://assetstore.unity.com/packages/3d/props/3d-wooden-chess-set-183336
2. https://assetstore.unity.com/packages/3d/props/furniture/table-with-chairs-x3-free-101246
3. https://assetstore.unity.com/packages/2d/textures-materials/4-snow-materials-high-quality-materials-collection-69201
4. https://assetstore.unity.com/packages/2d/textures-materials/wood/wooden-floor-materials-150564
5. https://assetstore.unity.com/packages/2d/textures-materials/sky/10-skyboxes-pack-day-night-32236
6. https://assetstore.unity.com/packages/tools/particles-effects/arrow-waypointer-22642
7. https://assetstore.unity.com/packages/3d/props/exterior/low-poly-brick-houses-131899
8. https://assetstore.unity.com/packages/3d/characters/low-poly-winter-pack-78938
9. https://freemusicarchive.org/music/Scott_Holmes/christmas-background-music/jingle-bells-2
