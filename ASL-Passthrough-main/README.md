# ASL Passthrough  
This project uses hand tracking and passthrough on the Quest 2 to detect single-handed ASL poses for letters and some generic signs such as pointing and thumbs up. It includes support for motion-based signs such as J and Z that require multiple poses over time.  This project was developed over the course of a week and a half.

![ASL_Letters](https://github.com/dillondrum70/ASL-Passthrough/assets/70776550/8ba03bee-2c25-453e-9dc0-4088bb1534f3)
  
## How It Works  
### Hand Pose
The main building blocks behind everything are the HandPoses. A pose is a snapshot in time of a hand and its joints positions and rotations.  This might be something like a thumbs up or stationary signs like the letter A.  Every frame, the current position and rotation of the users hand joints are checked against a list of known poses.  If each joint's rotation matches that of each joint in the known pose within some degree of tolerance, then that hand pose is stored and we move to HandGestures.  In the future, this pose reading may be given an independent frame rate such as 30 fps to improve performance.

### Hand Gesture
A HandGesture is a series of HandPoses with some additional data.  This can have one or more poses and can be thought of as the actual ASL sign.  During runtime, each time the user's hand makes a valid pose, a new HandPoseData struct with the current hand pose and a time float is pushed onto a stack.  To reduce space, if the current pose matches the one at the top of the stack, we just increase the time value of the element on top of the stack.  

Each gesture has a time value assigned to it that tells the system how long the *last* pose needs to be held for.  This way signs like J don't get confused with I as easily since J starts the same way as I.  If the current pose the user is making doesn't match the last pose in a HandGestures list and/or if the current pose hasn't been held for long enough, it can't possibly match with that HandGesture so there's no use iterating over its pose list.

![ASL_I](https://github.com/dillondrum70/ASL-Passthrough/assets/70776550/50901d35-8e84-4fbd-b59c-965068e04faa)

![ASL_J](https://github.com/dillondrum70/ASL-Passthrough/assets/70776550/035d09ce-9deb-4176-94eb-ac6efb4f23f9)

If the correct amount of time has elapsed and the current pose matches the last pose in a HandGesture, we iterate backwards through the list of poses in the HandGesture and make sure it aligns with the data in the stack of most recent hand poses.  If the HandGesture poses match the stack, we fire off any events and reset the stack.  
  
## Pose Tool  
I made it very easy to create and modify each HandPose by adding the ability to press a button at runtime in the editor to save the users current hand pose to a currently selected prefab.  I created one of these prefabs for each hand pose and store them in a list that can be iterated over.  Additional settings can be made, either ignoring the position where the sign is made (for something like a thumbs up perhaps) or ignoring the rotation of the wrist (if for example we wanted to know whenever a fist is made regardless of its orientation.)

This system saves a lot of time by allowing developers to quickly form new poses and gestures by moving their hands instead of manually editing the skeleton of a hand.  If small adjustments are needed, the hand pose can still be edited manually within the prefab.

## Challenges  
### Algorithms
This project was fairly challenging due to the algorithms I needed to created and the many different possible ways to detect what pose the hand is in.  I brainstormed several possible methods but settled on per-joint angles because it made the most sense and was the most accurate out of the options I drafted before starting the project.  A lot of time went into planning these systems out.

### Unsupported Finger Poses
The project itself was not the only challenge however.  Though Meta boasts some of the best Hand tracking to date, the software and Quest 2 are somewhat limited.  The thumb can not be covered by other fingers and finger-crossing is not supported.  This unfortunately means that M, N, R, and T are not possible to sign with Oculus hand tracking.  Even facing the hand towards the headset where the fingers are visible does nothing to fix the problem.  The best workaround I could think was to calibrate the pose by making the sign and storing whatever pose Oculus's hand tracking creates, but this causes M, N, T, and S to all be the same pose.  R Ends up being too inconsistent because the middle finger continues to move while the fingers are crossed.

### Occluded fingers
There were other difficulties with some of the letters due to the hand occluding the fingers as well.  K, Q, and S all involve some or all of the fingers to be hidden by the palm and other fingers if the pose faces away from the user.  In an effort to ensure the signs are accurate, I opted not to face those signs towards the player.  Hand tracking would have been improved, but at the cost of accuracy of the actual sign.  With more time, I would like to implement a training mode that allows users to analyze a pose from all angles then copy it while using the setting to ignore the rotation of their wrist.

## Applications
### Accessibility
The applications of this system vary greatly.  The intent of this project started as a simple game demo for spell casting, but pivoted to meet a need that is not often thought about in VR.  Users who are deaf or hard of hearing may have difficulty communicating through VR since most apps use voice over and do not have hand or face tracking.  The deaf community may miss out on a lot of interactions because of these missing features.  With this system or one like it, signs could be interpreted into text and displayed or read aloud to other users.  This could have applications in games or social applications such as VR Chat.

### Learning
Another way this could be used is as a learning tool.  Given more time, that is what this project would have evolved into.  By displaying different gestures and allowing users to complete them, this tool could help others learn sign language.

### Gameplay
This system can read any hand pose, not just ASL signs.  This could have applications in gesture-based magic, allowing players to cast spells with simple or complex hand motions.  This could also be used for something as simple as rock, paper, scissors or for finger guns.  As a demo, I created a script that allows you to spell out a known word ( I-C-E in this case) and cast a spell associated with that name.  These poses could easily be replaced with arbitrary gestures that may feel more magical.

![ASL_Ice](https://github.com/dillondrum70/ASL-Passthrough/assets/70776550/77ee1604-0e51-4008-8f70-0ed5d76bb8f9)