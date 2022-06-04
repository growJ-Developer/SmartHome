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


public class IoTController : MonoBehaviour {
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

    /* Protocol for send data */
    public enum Devices : byte {TV = 0x21, SPEAKER = 0x22}
    public enum Functions : byte {FUNC1 = 0x41, FUNC2 = 0x42}
    public enum Instructions : byte {INST1 = 0x61, INST2 = 0x62}

    void Start() {
        sp.Open();                                                          // Open the Serial Port
        sp.ReadTimeout = 50;
        sp.WriteTimeout = 50;

        bufferThread = new Thread(new ThreadStart(bufferAction));
        bufferThread.Start();
    }

    // Update is called once per frame
    void Update() {
        
        // Control Device Section
        if(Input.GetKey(KeyCode.Q)) {
            Debug.Log("Q");
            sendDataToMBed((byte)Devices.TV, (byte)Functions.FUNC1, (byte)Instructions.INST1);
        }
        if(Input.GetKey(KeyCode.W)) {
            Debug.Log("W");
            sendDataToMBed((byte)Devices.SPEAKER, (byte)Functions.FUNC2, (byte)Instructions.INST2);
        }

        if(Input.GetKey(KeyCode.E)) {
            Debug.Log("W");
            requestSensorData();
        }
    }

    // Buffer Action for buffer Thread
    void bufferAction(){
        while(true){
            if(isReqSensor){
                // Sensor Request Action
                getReceiveData();
            } else if(bufferIndex != outIndex){
                // If SensorRequest Byte, Waiting to ReadSensor
                if(buffer[outIndex] == ACK)     isReqSensor = true;
                // Send the Buffer Data 1Byte
                sp.Write(buffer, outIndex, 1);
                outIndex = (outIndex + 1) % bufferSize;
            }
        }
        
    }

    // Request the Sensor Data
    void requestSensorData(){
        // Send request signal to Mbed
        writeToBuffer(ACK);
    }

    // Send data to Mbed
    void sendDataToMBed(byte Device, byte Function, byte Command){
        writeToBuffer(STX);
        writeToBuffer(Device);
        writeToBuffer(DLE);
        writeToBuffer(Function);
        writeToBuffer(DLE);
        writeToBuffer(Command);
        writeToBuffer(ETX);
    }

    // Write to Buffer for Sending Data
    void writeToBuffer(byte data){
        buffer[bufferIndex] = data;
        bufferIndex = (bufferIndex + 1) % bufferSize;
    }

    // Get data from SerialPort
    void getReceiveData(){
        // If, Request the Sensor Data
        if(isReqSensor){
            try{
                Thread.Sleep(500);
                string[] readData = sp.ReadLine().Split(",");
                lightValue = int.Parse(readData[0]);
                irValue = int.Parse(readData[1]);
                humidityValue = int.Parse(readData[2]);
                celsiusValue = int.Parse(readData[3]);

                // If successful get sensor data, change thr isReqSensor to False
                isReqSensor = false;
                Debug.Log("Success the get sensor data.");
            } catch(System.TimeoutException e){
                // If, Don't have Received Data
            }
        }
    }

    public void CheckSerialPort(){
        if(sp.IsOpen){
            try{

            } catch(System.Exception e){
                bufferThread.Abort();
                print(e);
                sp.Close();
                throw;
            }
        }
    }
}
