import requests
from bs4 import BeautifulSoup
from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.common.by import By
from selenium.webdriver.chrome.options import Options
from webdriver_manager.chrome import ChromeDriverManager
import json
import time
import random

SCRAP_URL = "https://alkatreszek.hu/termekek/fek_mechanika/fekbetet/iveco/daily/daily_i_alvaznyitott_1978_01_1999_05/34050"
HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/154.0.0.0 Safari/537.36",
    "cookie": "molin_distinct_id=01a12fe3-2ded-73ce-8a86-bf1569dca566; _gcl_au=1.1.481269981.1791804779.1389233195.1791804783.1791804782.2015434999.1791804783.1791804782; _ga=GA1.1.1587582827.1791804784; _fbp=fb.1.1791804784494.22030407250717267; FPID=FPID2.2.Te1%2B0cRJ0oTTDkz0SQjWa2nV0RGYcRw7zdXQnbYiIP0%3D.1791804784; FPLC=mAUS2hGMLJGA%2FDv4C8gOOt1Ian6lS6qBULhotK7z%2Ff1IDFH5JrueCy6OKiNvBxtX9efTIKzdnyUfLgjsX68gpVtiueB9ZyxW5MY%2FKwnbQaP7M9l3lw84k%2BSxw8SogQ%3D%3D; FPAU=1.1.481269981.1791804779; _clck=1ngtl4f%5E2%5Ega8%5E1%5E2476; _hjSessionUser_3260636=eyJpZCI6IjA4NThiM2NmLWFmNDgtNTI0NC1hNjkyLTMwMWFiMDY3NmJlOSIsImNyZWF0ZWQiOjE3OTE4MDQ3NzkzNDAsImV4aXN0aW5nIjp0cnVlfQ==; cf_clearance=DRn9AfW01hRGt5rfDujurkXhIV5mx7LiNBR5YD458FM-1791810655-1.2.1.1-iL3TnbHEWOPRD931EzXCHmMgwkhjZ6CvGxJjlgGSvINMLxqaYpOX1lX833AleAQii8DUSBEPJ9H7LuN2fYKU6eYHEHbVnqBbe2m0OY8b6GkFVYFOTs.8fv0_hx0_72P1XT_Ys2QAj6yxkMiAd2BbKBeKso0FHhOoUMVvDGzWRuuBfLDQCz7l1TNv9KpEvXj_pejnR4uTJuWoQHW47.MdoMmdojaIQGv8NWvBU65Zyh086hmqUQzSVDEx8FvcOY0.04QYBc1uaDPqETWk3sL0hKM8.iQ3LX5tB4YkxN4nQ0DkALC_vrvE_rZCmw2oFoaHME9SgNCthD.MH9RU88ocvVQ4E.FZDevbjM4HuaF.zxc; _ga_TYLXT4QZ88=GS2.1.s1791810654$o2$g1$t1791810656$j58$l0$h343598084; FPGSID=1.1791810656.1791810656.G-TYLXT4QZ88.36CDnsnXhp5o_-dgS0EOKQ; _clsk=18uq3xa%5E1791810657257%5E3%5E1%5Ek.clarity.ms%2Fcollect; _hjSession_3260636=eyJpZCI6ImY3MTg5NjVlLWJmZDItNDQ4NC1hMjY5LWNmN2U2MTU3ZGEwMyIsImMiOjE3OTE4MTA2NTc1OTUsInMiOjAsInIiOjAsInNiIjowLCJzciI6MCwic2UiOjAsImZzIjowLCJzcCI6MX0="
}

try:
    options = webdriver.ChromeOptions()
    options.add_argument("--headless")
    options.add_argument("--disable-blink-features=AutomationControlled")
    options.add_experimental_option("excludeSwitches", ["enable-automation"])
    options.add_experimental_option('useAutomationExtension', False)

    driver = webdriver.Chrome(service=Service(ChromeDriverManager().install()), options=options)
    driver.execute_cdp_cmd("Network.setUserAgentOverride", {
        "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    })

    driver.get(SCRAP_URL)
    print("Scraping started")
    time.sleep(5)
    print("Scraping finished")

    pure_html_content = driver.page_source
    #print(pure_html_content)
    soup = BeautifulSoup(pure_html_content, 'html.parser')
    brakes = soup.find_all("tr", class_=["b-table-has-details", "b-table-details"])
    attributes = soup.find_all("div", class_="attribute__wrapper")
    print(f'Number of findings: {len(brakes)}')
    catalogue = []
    current_brake = {} #dictionary
    for brake in brakes:
        #print('---------------------------------\n')

        if 'b-table-has-details' in brake.get('class', []):
            #check for name and price
            brake_name = brake.find("div", class_="item-name").text.strip()
            current_brake['brake_name'] = brake_name
            #print(f'BRAKE NAME: {brake_name}\n')
            brake_item_number = brake.find("div", class_="item-link").text.strip().replace('Cikkszám: ', '')
            current_brake['brake_item_number'] = brake_item_number
            #print(f'BRAKE ITEM NUMBER: {brake_item_number}\n')
            brake_price = int(brake.find("div", class_="item-price").text.strip().replace(' Ft', '').replace(' ', ''))
            current_brake['brake_price'] = brake_price
            #print(f'BRAKE PRICE: {brake_price}\n')
        if 'b-table-details' in brake.get('class', []):
            #we do not need every detail, only brake-system, and manufacturer, so lets look for "Fékrendszer", and "Gyártó"
            attributes_list = brake.find_all("div", class_="attribute")

            for attr in attributes_list:
                attr_text = attr.text.strip()

                if 'Fékrendszer' in attr_text:
                    brake_system = attr_text.replace('Fékrendszer:', '').strip()
                    current_brake['brake_system'] = brake_system
                    #print(f'BRAKE SYSTEM: {brake_system}')

                if 'Gyártó' in attr_text:
                    manufacturer = attr_text.replace('Gyártó:', '').strip()
                    current_brake['manufacturer'] = manufacturer
                    #print(f'MANUFACTURER: {manufacturer}')

            if current_brake is not None:
                print(f'Current BRAKE: {current_brake}\n')
                catalogue.append(current_brake)
                print(f'Catalogue: {catalogue}\n')
                current_brake = {} #need to reset this, otherwise it will keep the previous values, and in the end the last brake will be here 20 times

    with open("brakes.json", "w", encoding="utf-8") as file:
        json.dump(catalogue, file, indent=4)
        print("Susccessfully saved catalogue")

    with open("C:/Intel/AutomotiveIndustryApp/VehicleTelemetryServer/brakes.json", "w", encoding="utf-8") as file:
        json.dump(catalogue, file, indent=4)
        print("Susccessfully saved catalogue2")


except Exception as e:
    print(f'Failed to reached web page {e}')