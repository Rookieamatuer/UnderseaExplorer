# README

### Description

Map design: In this project, I made a 2d map generator, which supports users to generate a map which corresponds to an undersea scene. During the map design, what I want to make is to separate the reef in the map and avoid procedure any hole in the reef. Besides, the boundary is also filled by reef, which ensure that agent would not be out of the map. In order to achieve this effect, I implemented the ProcessMap() function using the flood fill algorithm to identify various regions in the map. Also, by limiting the size of the trench region, filled all the trenches which blocked in the reef. The reef with less than ten grids is converted into a trench.

Agent design: In this project, there are two types of agent: Diver and Mermaid. In this project, the diver can go anywhere in this map, it has two types of behaviour: exploration and chasing mermaid. When there is no mermaid nearby, the diver will randomly pick a direction and move forward, repeat this behaviour in a fixed time; if the diver finds a mermaid, it will start to chase the mermaid. The mermaid can only move in the trench, and would do circle wandering when there is no diver nearby; if a diver is close enough, the mermaid will turn red and start to flee. Once the mermaid flees into a reef, it will disappear, and respawn after 5s.

### Video Link

https://youtu.be/aIe-o5EtKss
