# Slip Map Readme

## Slip Map Tool

This application was originally created to be used along side Star Generator v2.0, and is used to simulate a Slipstream network between star systems.

## Star Generator v2.0

Starwin is created by Aina Rasolomalala

# Projects
## .Net Framework Version
### WPF SlipMap

This is the original desktop application. This UI can be used to
navigate the slip map for a sector.

### SlipMap Code Library

This contains the original logic and model for the project and the
ability to save to binary save files.

## .Net Framework Rewrite Version
While trying to rewrite the code in .NET 6 I discovered that it is 
currently hard interpet my original code during to convert to .NET 6. 
And since the .NET 6 Version will be introducing a bunch of new features,
I wanted to create a set of intermediate projects for converting from the 
original code, to the new system while allowing for the possibility of
upgradeing project files.

### SlipMap.NetFramework.Rewrite.Model
This project is a extract and update of the model from the original 
"SlipMap Code Library"

Changes from Original Model:

- Slip Drive is now Ship with new tracked variables
- Star Systems reference each other by ID instead of directly (Json Recursion fix)
- Slip Map System list is ignored for JSON Serialization
    - Star Systems will be saved in individual safe files for performance purposes
    - Slip Map is now Sector Map

### SlipMap.NetFramework.Rewrite.Domain
This project is a extract and update of the business logic from 
the original "Slipmap Code Library"

Changes from Original Business Logic

- Binary Save Files Changed to Json
- Star Systems are saved as seporate files from Sector Map in Sub Folder

### DeveloperRewriteLogicTesting
Just a test harness for making sure the logic is working as expected.

## Net 6 Rewrite
TODO: Update this