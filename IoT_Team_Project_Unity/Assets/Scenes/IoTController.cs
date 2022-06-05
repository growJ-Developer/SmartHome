using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System.Text;
using System.Threading;

/* How To Check the Serial Port?
/*  If use the mac, turn on the Terminal.
/*   wirte the command 'ls /dev/tty.*
/*   then, you can see like '/dev/tty.usbmodem1403'
/*
/*  If use the windows, search on device manager
*/
public class IoTController : MonoBehaviour
{
    public Thread spThread;
    SerialPort sp = new SerialPort("/dev/tty.usbmodem1403", 115200, Parity.None, 8, StopBits.None);     // Open Serial Port(Mac)
    //SerialPort sp = new SerialPort("COM4", 115200, Parity.None, 8, StopBits.None);                    // Open Serial Port(Windows)

    public static byte STX = 0x02;                                           // STX byte data
    public static byte DLE = 0x01;                                           // DLE byte data
    public static byte ETX = 0x03;                                           // ETX byte data
    public static byte ACK = 0x06;                                           // ACK byte data
    public static int receiveIndex = 0;                                      // Receive Index

    public static bool isReqSensor = false;
    public static int bufferSize = 255;
    public static byte[] buffer = new byte[bufferSize];
    public static int bufferIndex = 0;
    public static int outIndex = 0;
    public static Thread bufferThread;

    /* Sensor Data */
    public static int lightValue = 0;
    public static int irValue = 0;
    public static int humidityValue = 0;
    public static int celsiusValue = 0;

    /* AutoController Variable */
    public static bool acAuto = false;
    public static bool blindAuto = false;
    public static bool lightAuto = false;


    /* Protocol for send data */
    public enum Devices : byte { TV = 0x21, SPEAKER = 0x22, ACPower = 0x23, ACTemp = 0x24, BLIND = 0x25, LIGHT = 0x26 }
    public enum Functions : byte { FUNC1 = 0x41, FUNC2 = 0x42, FUNC3 = 0x43, FUNC4 = 0x44 }
    public enum Instructions : byte { INST1 = 0x61, INST2 = 0x62, INST3 = 0x63 }

    void Start()
    {
        sp.Open();                                                          // Open the Serial Port
        sp.ReadTimeout = 50;
        sp.WriteTimeout = 100;

        bufferThread = new Thread(new ThreadStart(bufferAction));
        bufferThread.Start();
    }

    // Update is called once per frame
    void Update()
    {

        // Control Device Section
        if (Input.GetKey(KeyCode.E))    requestSensorData();
        if (Input.GetKey(KeyCode.Q))    sendDataToMBed((byte)Devices.TV, (byte)Functions.FUNC1, (byte)Instructions.INST1);
        if (Input.GetKey(KeyCode.W))    sendDataToMBed((byte)Devices.SPEAKER, (byte)Functions.FUNC2, (byte)Instructions.INST2);
        if (Input.GetKey(KeyCode.A)) {
            acAuto = false;
            sendDataToMBed((byte)Devices.ACPower, (byte)Functions.FUNC3, (byte)Instructions.INST1);
        }   
        if (Input.GetKey(KeyCode.S)) {
            acAuto = false;
            sendDataToMBed((byte)Devices.ACPower, (byte)Functions.FUNC4, (byte)Instructions.INST1);
        }

        if (Input.GetKey(KeyCode.D)) {
            acAuto = false;
            sendDataToMBed((byte)Devices.ACTemp, (byte)Functions.FUNC3, (byte)Instructions.INST1);
        }

        if (Input.GetKey(KeyCode.F)) {
            acAuto = false;
            sendDataToMBed((byte)Devices.ACTemp, (byte)Functions.FUNC4, (byte)Instructions.INST1);
        }

        if (Input.GetKey(KeyCode.Z)) {
            blindAuto = false;
            sendDataToMBed((byte)Devices.BLIND, (byte)Functions.FUNC1, (byte)Instructions.INST1);
        }

        if (Input.GetKey(KeyCode.X)) {
            blindAuto = false;
            sendDataToMBed((byte)Devices.BLIND, (byte)Functions.FUNC1, (byte)Instructions.INST2);
        }
        
        if (Input.GetKey(KeyCode.V)) {
            lightAuto = false;
            sendDataToMBed((byte)Devices.LIGHT, (byte)Functions.FUNC1, (byte)Instructions.INST1);
        }

        if (Input.GetKey(KeyCode.B)) {
            lightAuto = false;
            sendDataToMBed((byte)Devices.LIGHT, (byte)Functions.FUNC1, (byte)Instructions.INST2);
        }
        
        if (Input.GetKey(KeyCode.G))    acAuto = !acAuto;
        if (Input.GetKey(KeyCode.C))    blindAuto = !blindAuto;
        if (Input.GetKey(KeyCode.N))    lightAuto = !lightAuto;

        if (blindAuto)              autoBlindAction();
        if (acAuto)                 autoAcAction();
        if (lightAuto)              autoLightAction();
    }

    /* Auto Controller for Aircondition */
    void autoAcAction(){
        if (celsiusValue > 24)      sendDataToMBed((byte)Devices.ACTemp, (byte)Functions.FUNC2, (byte)Instructions.INST2);
        else                        sendDataToMBed((byte)Devices.ACTemp, (byte)Functions.FUNC2, (byte)Instructions.INST3);
    }
    /* Auto Controller for Blind */
    void autoBlindAction() {
        if (lightValue > 50000)     sendDataToMBed((byte)Devices.BLIND, (byte)Functions.FUNC1, (byte)Instructions.INST1);
        else                        sendDataToMBed((byte)Devices.BLIND, (byte)Functions.FUNC1, (byte)Instructions.INST2);
    }
    /* Auto Controller for Light(Mood) */
    void autoLightAction() {
        if (lightValue > 50000)     sendDataToMBed((byte)Devices.LIGHT, (byte)Functions.FUNC1, (byte)Instructions.INST1);
        else                        sendDataToMBed((byte)Devices.LIGHT, (byte)Functions.FUNC1, (byte)Instructions.INST2);
    }

    // Buffer Action for buffer Thread
    void bufferAction() {
        while (true) {
            if (isReqSensor)    getReceiveData();
            else if (bufferIndex != outIndex){
                Debug.Log("Send!");
                // If SensorRequest Byte, Waiting to ReadSensor
                if (buffer[outIndex] == ACK) isReqSensor = true;
                // Send the Buffer Data 1Byte
                sp.Write(buffer, outIndex, 1);
                outIndex = (outIndex + 1) % bufferSize;
            }
        }
    }

    // Request the Sensor Data
    void requestSensorData() {
        // Send request signal to Mbed
        writeToBuffer(ACK);
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

            // If successful get sensor data, change thr isReqSensor to False
            isReqSensor = false;
            outIndex = (outIndex + 1) % bufferIndex;
            Debug.Log("Success the get sensor data.");
        } catch (System.TimeoutException) { }
    }
}
