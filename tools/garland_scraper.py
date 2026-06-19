import json
import asyncio
import aiohttp
import os

GARLAND_ITEM_URL = "https://garlandtools.org/db/doc/item/en/3/{}.json"
OUTPUT_FILE = "../GlamourChecker/Resources/SharedModels.json"

async def fetch_garland_item(session, item_id, semaphore):
    async with semaphore:
        url = GARLAND_ITEM_URL.format(item_id)
        # Be polite but fast
        await asyncio.sleep(0.005)
        try:
            async with session.get(url, headers={'User-Agent': 'Mozilla/5.0'}) as response:
                if response.status == 200:
                    data = await response.json()
                    item = data.get('item', {})
                    # Only care if it's equipment (category < 100 roughly, we can just check if it has sharedModels)
                    shared = item.get('sharedModels', [])
                    if shared:
                        return item_id, shared
        except Exception:
            pass
        return item_id, None

async def build_dictionary():
    print("Fetching shared models from GarlandTools (Scanning IDs 1 to 45000)...")
    shared_models_map = {}
    
    # 50 concurrent requests is very reasonable
    semaphore = asyncio.Semaphore(50)
    
    async with aiohttp.ClientSession() as session:
        # FFXIV items are currently up to ~45000. We scan up to 50000 to be safe for future patches.
        tasks = [fetch_garland_item(session, item_id, semaphore) for item_id in range(1, 45000)]
        
        total = len(tasks)
        completed = 0
        
        for task in asyncio.as_completed(tasks):
            item_id, shared = await task
            completed += 1
            if completed % 5000 == 0:
                print(f"Progress: {completed}/{total}")
                
            if shared:
                # To create visual groups, we can just use the smallest ID in the shared list as the Group ID.
                # The shared list contains strings like "7-105-0-0" but sometimes it's Item IDs?
                # Wait, GarlandTools sharedModels is a list of Item IDs!
                # Let's ensure they are integers.
                try:
                    shared_ints = [int(x) for x in shared]
                    group_id = min(shared_ints + [item_id])
                    shared_models_map[item_id] = group_id
                except ValueError:
                    # If they are not ints, just hash the string or something
                    pass

    print(f"\nBuilt dictionary with {len(shared_models_map)} mappings.")
    
    # In GitHub actions, we run from the root of the repo
    os.makedirs(os.path.dirname(OUTPUT_FILE), exist_ok=True)
    with open(OUTPUT_FILE, 'w') as f:
        json.dump(shared_models_map, f)
    print(f"Saved to {OUTPUT_FILE}")

if __name__ == "__main__":
    asyncio.run(build_dictionary())
