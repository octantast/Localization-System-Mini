Link to current documentation: https://github.com/octantast/Localization-System-Mini/blob/main/README.md

1. Import the asset into your Unity project.  

For reference: 

2. Add the ExampleTextTooltip.prefab to your scene. Run the scene to see how text tooltip retrieves data from the table and the example component. You can substitute any of your own values ​​for the key and associated component.
3. Open the TutorialInputStrings.csv table and see how string keys and translations are related. Fill the table with your keys and translations.
4. Explore the UI flag buttons on the demo scene for switching languages ​​at runtime. Use the same methods for translations using your buttons. Note that many operations are automatic and only require a reference to the InputData scriptable object.
5. Open InputData scriptable object where are languages ​​and fonts stored. Replace the default font with your own.

Similarly, use the system for your objects. 
For quick setup:

6.Add a LocalizedTooltip to your object and assign it`s text object + a key to the table row. (If the object has a component with variables (e.g., card strength, character speed, etc.), you can add reference to the tooltips parameter list and use texts with {} placeholders).

Use this system for text cues, such as combat skill descriptions, playing card or location labels, even text in menus or under icons.