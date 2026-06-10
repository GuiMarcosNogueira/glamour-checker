import json

with open('C:/Users/Illidan/AppData/Roaming/XIVLauncher/pluginConfigs/GlamourChecker.json', 'r') as f:
    data = json.load(f)

print("All items in DresserItemsBySharedModel:")
for k, v in data.get('DresserItemsBySharedModel', {}).items():
    if type(v) == list:
        print(f"{v}")
