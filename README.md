### What is this?
This is a fork of the Scriptable Auto Splitter component for LiveSplit. It uses the Windows Multimedia Timer API to be able to update at more precise intervals than C#/.Net's built in Timer classes are capable of. 

#### Pros
* refreshRate actually can work the way people think it does, and have a meaningful impact with values up to 1000.
* Reduced variance in time between script updates.

#### Cons
* Significantly increased power consumption, reduced overall system performance, and reduced battery life while this component is loaded.
* The multimedia timer API is labelled as "obsolete" in its documentation and could be removed at any time for all I know.
* If the interval of the timer is set very low and a script takes longer than the interval to execute, the UI thread will have no chance to do anything other than handle script updates and the UI will lock.
* I haven't thoroughly tested this. It might not even work for you. Use at your own risk.
