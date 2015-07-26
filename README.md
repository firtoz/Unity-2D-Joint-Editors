# Joint Editors 2D
This package aims to make it easier for you to use 2D joints in Unity3D.

Made by Firtina ([@ToxicFork](https://twitter.com/toxicfork)) Ozbalikci.

License: MIT ( check MIT.txt )

## Features
- Much better UX!
    - Interactive elements are shown clearly and the cursor changes based on context:
    - ![Move](http://i.imgur.com/wJ4qVTv.png)
    - ![Limit Max](http://i.imgur.com/axtBzRJ.png)
    - ![Limit Min](http://i.imgur.com/kNEyGns.png)
    - ![Angle](http://i.imgur.com/j9YR2hT.png)
- Customizable visual handles for anchor and joint limit configuration
    - Check out the data/settings asset for visual and other configuration :D
    - ![Settings](http://i.imgur.com/FD7p3j7.png)
- Context menu on anchors, just right-click the anchor or the limit handles!
- ![Context menu](http://i.imgur.com/BYwmNRp.png)
- Anchor locking e.g. when you move an anchor, the other one moves with it
    - Hold shift and click on any anchor to toggle locking
    - ![Anchor Locking](http://i.imgur.com/PegXgC8.png)
- Drag any object reference onto the main/connected anchor handles, and it will
 auto-magically be connected, as long as it has a RigidBody2D component
- Hold control while dragging the handles, this allows you to snap the anchors and limits
  - The snapping positions are highlighted by circles
  - ![Snap](http://i.imgur.com/SnSPnvs.png)
- Can see transparent outlines of all joints connected to the selected object
    - ![Outline](http://i.imgur.com/qBJ5riG.png)
    - Configurable via Settings -> Connected Joints
- Hinge Joint 2D:
    - Object movement circles:  
    - ![Object Movement Circle](http://i.imgur.com/5dAko4r.png)
    - Better limits display (can click and drag the lower/upper limit indicators):
    - ![Better limits display](http://i.imgur.com/wpBmoKh.png)
    - Hold control to snap to nice angles!
- Slider / Wheel Joint 2D:
    - Configure and see angle visually
    - ![Slide Angle](http://i.imgur.com/j9YR2hT.png)
- Slider Joint 2D:
    - Can see and edit limits visually  
    - ![Limit Max](http://i.imgur.com/axtBzRJ.png)
    - ![Limit Min](http://i.imgur.com/kNEyGns.png)
- Spring / Distance Joint 2D:
    - Configure distance visually:
    - ![Distance](http://i.imgur.com/xt2j3Tv.png)
