import requests
import pandas as pd
import matplotlib.pyplot as plt
import streamlit as st

URL = "https://localhost:7196/vehicletelemetry/failures"

#Local Visualisation
try:
    response = requests.get(URL, verify=False)
    print(f"C# response: {response.status_code}")
    data = response.json()["summary"]
    df = pd.DataFrame(data)

    plt.bar(df["vehicle"], df["count"], color="royalblue", edgecolor="black")
    plt.title("Failure Stats Dashboard")
    plt.xlabel("Vehicles")
    plt.ylabel("Number of Failures")
    plt.show()

except Exception as e:
    print(f"Error during requesting data: {e}")


#Online Visualisation
st.set_page_config(page_title="Python Telemetry Dashboard", layout="wide")
st.title("📊 Failure Stats - Python Dashboard")
st.subheader("Live database report from C# Web API")

try:
    response = requests.get(URL, verify=False)

    if response.status_code == 200:
        data = response.json()["summary"]

        df = pd.DataFrame(data)

        #These below are also the names which we gave in C# SQL Query Command
        chart_data = df.pivot(index="vehicle", columns="failure", values="count").fillna(0)

        #Draw the chart
        st.bar_chart(chart_data) #chart
        st.write("Summary Data Table:")
        st.dataframe(chart_data) #table

    else:
        st.error(f"Server Error! HTTP status: {response.status_code}")

except Exception as e:
    st.error(f"Connection/Network issue during data request: {e}")
    st.warning("Make sure your system is live and running!")