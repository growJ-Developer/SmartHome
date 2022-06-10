using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using UnityEngine;

public class MainController : MonoBehaviour
{
    public static MainController instance;

    public Thread spThread;
    SerialPort sp = new SerialPort("/dev/tty.usbmodem11403", 115200, Parity.None, 8, StopBits.None);     // Open Serial Port(Mac)
    //SerialPort sp = new SerialPort("COM4", 115200, Parity.None, 8, StopBits.None);                    // Open Serial Port(Windows)

    public static float sensorTime;
    public static float sensorDuration = 0.05f;

    public static byte STX = 0x02;                                           // STX byte data
    public static byte DLE = 0x01;                                           // DLE byte data
    public static byte ETX = 0x03;                                           // ETX byte data
    public static byte ACK = 0x06;                                           // ACK byte data
    public static int receiveIndex = 0;                                      // Receive Index

    public static bool isReqSensor = false;
    public static int bufferSize = 2048;
    public static byte[] buffer = new byte[bufferSize];
    public static int bufferIndex = 0;
    public static int outIndex = 0;
    public static Thread bufferThread;

    /* Sensor Data */
    public static int lightValue = 0;
    public static int irValue = 0;
    public static int humidityValue = 0;
    public static int celsiusValue = 0;
    public static bool isBlind = false;
    public static bool isLight = false;
    public static bool isAC = false;
    public static bool isTV = false;
    public static bool isSink = false;
    public static int tempLevel = 3;
    public static int powerLevel = 0;
    public static int[] tempArr = {20, 22, 24, 26, 28};

    /* Protocol for send data */
    public enum Devices : byte { TV = 0x21, SINK = 0x22, AC = 0x23, BLIND = 0x24, LIGHT = 0x25 }
    public enum Functions : byte { FUNC1 = 0x41, FUNC2 = 0x42, FUNC3 = 0x43, FUNC4 = 0x44 }
    public enum Instructions : byte { INST1 = 0x61, INST2 = 0x62, INST3 = 0x63 }
    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        sp.Open();                                                          // Open the Serial Port
        sp.ReadTimeout = 100;
        sp.WriteTimeout = 100;

        bufferThread = new Thread(new ThreadStart(bufferAction));
        bufferThread.Start();   
    }

    // Update is called once per frame
    void Update()
    {
        sensorTime += Time.deltaTime;
        /* Request the Sensor data from mbed */
        if(sensorTime > sensorDuration){
            if(!isReqSensor){
                writeToBuffer(ACK);
                sensorTime = 0;
            }
        }
    }

    public void sinkAction(){
        if(isSink)      sendDataToMBed((byte)Devices.SINK, (byte)Functions.FUNC1, (byte)Instructions.INST1);
        else            sendDataToMBed((byte)Devices.SINK, (byte)Functions.FUNC1, (byte)Instructions.INST2); 
    }
    public void lightAction(){
        if(isLight)     sendDataToMBed((byte)Devices.LIGHT, (byte)Functions.FUNC1, (byte)Instructions.INST1);
        else            sendDataToMBed((byte)Devices.LIGHT, (byte)Functions.FUNC1, (byte)Instructions.INST2); 
    }
    public void blindAction(){
        if(isBlind)     sendDataToMBed((byte)Devices.BLIND, (byte)Functions.FUNC1, (byte)Instructions.INST1);
        else            sendDataToMBed((byte)Devices.BLIND, (byte)Functions.FUNC1, (byte)Instructions.INST2);
    }
    public void acPowerAction(){
        if(isAC)        sendDataToMBed((byte)Devices.AC, (byte)Functions.FUNC1, (byte)Instructions.INST1);
        else            sendDataToMBed((byte)Devices.AC, (byte)Functions.FUNC1, (byte)Instructions.INST2);
    }
    public void tvAction(){
        if(isTV)        sendDataToMBed((byte)Devices.TV, (byte)Functions.FUNC1, (byte)Instructions.INST1);
        else            sendDataToMBed((byte)Devices.TV, (byte)Functions.FUNC1, (byte)Instructions.INST2);
    }
    public void acFanDownAction(){
        if(isAC)        sendDataToMBed((byte)Devices.AC, (byte)Functions.FUNC2, (byte)Instructions.INST1);
    }
    public void acFanUpAction(){
        if(isAC)        sendDataToMBed((byte)Devices.AC, (byte)Functions.FUNC2, (byte)Instructions.INST2);
    }
    public void acTempDownAction(){
        if(isAC)        sendDataToMBed((byte)Devices.AC, (byte)Functions.FUNC3, (byte)Instructions.INST1);
    }
    public void acTempUpAction(){
        if(isAC)        sendDataToMBed((byte)Devices.AC, (byte)Functions.FUNC3, (byte)Instructions.INST2);
    }
    public bool isTVOn(){
        return isTV;
    }
    public bool isACOn(){
        return isAC;
    }
    public bool isBlindOn(){
        return isBlind;
    }
    public bool isMoodOn(){
        return isLight;
    }
    public bool isSinkOn(){
        return isSink;
    }
    public int getCelcius(){
        return celsiusValue;
    }
    public int getHumidity(){
        return humidityValue;
    }
    public int getSetTemp(){
        return tempArr[tempLevel];
    }
    public int getPowerLevel(){
        return powerLevel;
    }


    // Buffer Action for buffer Thread
    void bufferAction() {
        while (true) {
            if(isReqSensor)     getReceiveData();
            else if (bufferIndex != outIndex){
                // If SensorRequest Byte, Waiting to ReadSensor
                if (buffer[outIndex] == ACK) {
                    Debug.Log("Request the Sensor");
                    isReqSensor = true;
                }
                // Send the Buffer Data 1Byte
                sp.Write(buffer, outIndex, 1);
                outIndex = (outIndex + 1) % bufferSize;
            }
        }
    }
    // Send data to Mbed
    void sendDataToMBed(byte Device, byte Function, byte Command) {
        writeToBuffer(STX);
        writeToBuffer(Device);
        writeToBuffer(DLE);
        writeToBuffer(Function);
        writeToBuffer(DLE);
        writeToBuffer(Command);
        writeToBuffer(ETX);
    }

    // Write to Buffer for Sending Data
    void writeToBuffer(byte data) {
        buffer[bufferIndex] = data;
        bufferIndex = (bufferIndex + 1) % bufferSize;
    }

    // Get data from SerialPort
    void getReceiveData() {
        try {
            string[] readData = sp.ReadLine().Split(",");

            lightValue = int.Parse(readData[0]);
            irValue = int.Parse(readData[1]);
            humidityValue = int.Parse(readData[2]);
            celsiusValue = int.Parse(readData[3]);
            tempLevel = int.Parse(readData[4]);
            isBlind = int.Parse(readData[5]) == 1 ? true : false;
            isLight = int.Parse(readData[6]) == 1 ? true : false;
            isAC = int.Parse(readData[7]) == 1 ? true : false;
            isTV = int.Parse(readData[8]) == 1 ? true : false;
            powerLevel = int.Parse(readData[9]);
            isSink = int.Parse(readData[10]) == 1 ? true : false;

            // If successful get sensor data, change thr isReqSensor to False
            isReqSensor = false;
            Debug.Log(string.Format("Updated Status. SENSOR : ({0}, {1}, {2}, {3}) / Mood : {4}, Blind : {5}, AC : {6}, TV : {7}, Fan : {8}, isSink : {9}", lightValue, irValue, humidityValue, celsiusValue, isLight, isBlind, isAC, isTV, powerLevel, isSink));
        } catch (System.TimeoutException) {

        }
    }
}
