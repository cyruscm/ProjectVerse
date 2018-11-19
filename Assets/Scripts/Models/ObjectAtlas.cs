using System;
using System.Collections.Generic;

public class ObjectAtlas {
    private static Dictionary<String, ThingDef> objectAtlas;

    public static void InitializeAtlas() {
        if (objectAtlas == null) {
            createAtlas();
        }
    }

    public static ThingDef getObject(String objectName) {
        InitializeAtlas();
        return objectAtlas[objectName];
    }

    private static void createAtlas() {
        ThingDef barrel = new ThingDef("core.barrel", "Sprites/Objects/barrel");
        ThingDef trigger = new ThingDef("core.trigger", "Sprites/Terrain/Collision");
        objectAtlas = new Dictionary<string, ThingDef>();
        objectAtlas.Add(barrel.name, barrel);
        objectAtlas.Add(trigger.name, trigger);
    }
    
}
