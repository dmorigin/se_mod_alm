## Airlock Manager

This is a simple manager for your airlocks. All he does is turn the doors on and off and open and close them. The 
trigger for the processing chain is opening one of the doors of an airlock.

#### How to get it

At the moment the script is not available in the workshop, because I mainly use it myself and revise and extend 
it bit by bit if I feel like it. If you still want to try it, no problem. You will find a file called: genereted.cs 
in the main directory. Just copy the whole content of this file.

#### How to Setup

It's very simple. Name each of your doors according to the following principle: Airlock Name. Where "Name" is 
replaced with the name of your airlock. All doors with the same name are automatically considered part of the 
airlock.

In the simplest example, you have two doors. You want to use them as airlocks and you want to give them the 
name: "Landing Platform A". Then name both doors as follows: "Airlock Landing Platform A." Now you have an airlock.

#### Configuration

Yes, it is possible to configure a few little things. However, this is purely optional. The configuration is done 
in the CustomData field of the PB in which the Airlock Manager runs. The formatting corresponds to the INI file 
format. Each airlock can be configured individually. The name of the airlock corresponds to the name of the section.

An example. You want to configure the airlock created above. Then the syntax is as follows:
```
[Airlock Landing Platform A]
key=value
```

You can set the following two things:

* delay: Specifies the delay in ms between closing one side of the airlock and opening the other. The default value is 1000ms.
* automatic: Here you can select the automatic mode. There are 3 modes available.
  * Full: In this mode everything is automatic. Is default value.
  * Half: In this mode all other doors are locked and unlocked, but not automatically opened. 
  * Manual: In this mode everything has to be done manually.