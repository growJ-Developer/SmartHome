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
    //SerialPort sp = new SerialPort("COM4", 115200, Parity.None, 8, StopBits.None);                      // Open Serial Port(Windows)

    public static byte STX = 0x02;                                           // STX byte data
    public static byte DLE = 0x01;                                           // DLE byte data
    public static byte ETX = 0x03;                                       // ETX byte data
    public static int receiveIndex = 0;                                      // Receive Index

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
        sp.ReadTimeout = 100;
    }

    // Update is called once per frame
    void Update() {
        getReceiveData();                                                  // Gat data from SerialPort

        // Control Device Section
        if(Input.GetKey(KeyCode.Q)) {
            Debug.Log("Q");
            sendDataToMBed((byte)Devices.TV, (byte)Functions.FUNC1, (byte)Instructions.INST1);
        }
        if(Input.GetKey(KeyCode.W)) {
            Debug.Log("W");
            sendDataToMBed((byte)Devices.SPEAKER, (byte)Functions.FUNC1, (byte)Instructions.INST1);
        }
    }

    // Send data to Mbed
    void sendDataToMBed(byte Device, byte Function, byte Command){
        // Processing the send data
        byte[] Protocol = {STX, Device, DLE, Function, DLE, Command, ETX};
        // Send data to MBed (STX-ETX Format)
        sp.Write(Protocol, 0, Protocol.Length);              // Send the Protocol Data
    }

    // Get data from SerialPort
    void getReceiveData(){
        try{
            string[] readData = sp.ReadLine().Split(",");

            lightValue = int.Parse(readData[0]);
            irValue = int.Parse(readData[1]);
            humidityValue = int.Parse(readData[2]);
            celsiusValue = int.Parse(readData[3]);
        } catch(System.TimeoutException){

        }
    }

    public void CheckSerialPort(){
        if(sp.IsOpen){
            try{

            } catch(System.Exception e){
                print(e);
                sp.Close();
                throw;
            }
        }
    }
}
