# FMOD-MIDI-Control

Control FMOD's Mixer and Event Parameters by using any MIDI Device. "Record" a playing Event into a new Event. 

<b>Usage:</b>

1) Select your favorite MIDI Device from the drop down list:

<img src=https://i.imgur.com/521PVvV.png></img>

2) If you want to control Event Parameters, click on the <b>Event Refresh</b> button while having an Event selected in the Event Browser.
You can enter CC values manually or use the <b>Learn</b> buttons to detect a midi event. You can record a playing Event by pressing the entered resampling CC value.
One such message will start the recording, the second one will stop it and create a new Event based on that. 

<img src=https://i.imgur.com/bHloHwy.png></img>

3) If you want to control the volume of your mixer busses, click on the <b>Mixer Refresh</b> button. You will get a list of busses
with their corresponding MIDI CC value. Enter the CC values manually or by using the Learn buttons. To save a mixer template,
click on save.

<img src=https://i.imgur.com/bu9BMaZ.png></img>


TO DO: The resample feature will create empty profiler sessions. You need to remove these manually for now.
