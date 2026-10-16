import json
import time
import random
from datetime import datetime

import requests

VEHICLE_ID = ["VEHICLE_01", "VEHICLE_02", "VEHICLE_03", "VEHICLE_04", "VEHICLE_05"]
VEHICLES =[]

with open("brakes.json", "r", encoding="utf-8") as file:
    brakes = json.load(file)

for x in range(len(VEHICLE_ID)):
    current_vehicle = {}
    rnd = random.randint(0, len(brakes)-1)
    current_vehicle["Name"] = VEHICLE_ID[x]
    current_vehicle["Brake"] = brakes[rnd]['brake_name']
    #Setting default values for datas, that we will generate later on
    current_vehicle["velocity"] = 100.00 #kph
    current_vehicle["brake_pedal_position"] = 0 #%
    current_vehicle["brake_pedal_temperature"] = 25.0
    current_vehicle["timestamp"] = ""
    VEHICLES.append(current_vehicle)

#print(VEHICLES)

BRAKING_CYCLE = False
BRAKING_CYCLE_LOOP_COUNTER = 0
NO_BRAKING_CYCLE_LOOP_COUNTER = 0
NEW_RANDOM_GEN_NEEDED = True
LAST_LOOP_VELOCITY = [100.00] * len(VEHICLE_ID)
LAST_LOOP_BR_PED_POS = [0] * len(VEHICLE_ID)
LAST_LOOP_BR_PED_TEMP = [25.0] * len(VEHICLE_ID)
while True:
    time.sleep(0.01) #10ms loops are standard in automotive

    if NEW_RANDOM_GEN_NEEDED:
        braking_cycle_length = random.randint(20, 100) #generating length for brake cycle between 0.2 and 1 sec
        cooldown_cycle_length = random.randint(20, 100) # generating length for brake release and cooldown between 0.2 and 1 sec
        NEW_RANDOM_GEN_NEEDED = False
        BRAKING_CYCLE_LOOP_COUNTER = 0
        NO_BRAKING_CYCLE_LOOP_COUNTER = 0
        BRAKING_CYCLE = True

    for id, vehicle in enumerate(VEHICLES):

        vehicle["timestamp"] = datetime.now().strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]
        LAST_LOOP_VELOCITY[id] = vehicle["velocity"]
        LAST_LOOP_BR_PED_POS[id] = vehicle["brake_pedal_position"]
        LAST_LOOP_BR_PED_TEMP[id] = vehicle["brake_pedal_temperature"]

        if BRAKING_CYCLE_LOOP_COUNTER < braking_cycle_length:
            if id == 0: #without this, we would increase it 5 times in a loop, because we have 5 vehicles. This way we only increase it when we enter the first (index 0) vehicle
                BRAKING_CYCLE_LOOP_COUNTER += 1

            deceleration = random.random() #it will generate for us a number between 0 and 1
            if LAST_LOOP_VELOCITY[id] - deceleration < 0:
                vehicle["velocity"] = 0
            else:
                vehicle["velocity"] = LAST_LOOP_VELOCITY[id] - deceleration

            pedal_gradient = random.randint(2,10)
            if LAST_LOOP_BR_PED_POS[id] + pedal_gradient > 100:
                vehicle["brake_pedal_position"] = 100
            else:
                vehicle["brake_pedal_position"] = LAST_LOOP_BR_PED_POS[id] + pedal_gradient

            temp_gradient = random.random()
            vehicle["brake_pedal_temperature"] = LAST_LOOP_BR_PED_TEMP[id] + temp_gradient

        elif(NO_BRAKING_CYCLE_LOOP_COUNTER < cooldown_cycle_length):
            if id == 0:
                NO_BRAKING_CYCLE_LOOP_COUNTER += 1

            acceleration = random.random()
            if LAST_LOOP_VELOCITY[id] + acceleration > 100.0:
                vehicle["velocity"] = 100.0
            else:
                vehicle["velocity"] = LAST_LOOP_VELOCITY[id] + acceleration

            release_gradient = random.randint(5, 15)
            if LAST_LOOP_BR_PED_POS[id] - release_gradient < 0:
                vehicle["brake_pedal_position"] = 0
            else:
                vehicle["brake_pedal_position"] = LAST_LOOP_BR_PED_POS[id] - release_gradient

            cool_gradient = random.random()
            if LAST_LOOP_BR_PED_TEMP[id] - cool_gradient < 25.0:
                vehicle["brake_pedal_temperature"] = 25.0
            else:
                vehicle["brake_pedal_temperature"] = LAST_LOOP_BR_PED_TEMP[id] - cool_gradient
        else:
            NEW_RANDOM_GEN_NEEDED = True

        vehicle["braking_cycle"] = BRAKING_CYCLE
        vehicle["braking_loop_counter"] = BRAKING_CYCLE_LOOP_COUNTER
        vehicle["no_braking_loop_counter"] = NO_BRAKING_CYCLE_LOOP_COUNTER

        print(f'{vehicle}\n')

        URL = "https://localhost:7196/vehicletelemetry"
        try:
            response = requests.post(URL, json=vehicle, verify=False)
            print(f"C# response: {response.status_code}")
        except Exception as e:
            print(f"Error during sending data: {e}")
