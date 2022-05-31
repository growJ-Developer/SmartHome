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
    public SerialPort sp = new SerialPort("/dev/tty.usbmodem1403", 115200, Parity.None, 8, StopBits.None);     // Open Serial Port(Mac)
    //SerialPort sp = new SerialPort("COM4", 115200, Parity.None, 8, StopBits.None);                      // Open Serial Port(Windows)
    
    public static byte[] buffer = new byte[sizeof(int)];                            // Byte Array for receive Serial Data
    public static int bufferSize = sizeof(int);
    public static int bufferIndex = 0;
    public static byte STX = (byte) 0x02;                                           // STX byte data
    public static byte ETX = (byte) 0x03;                                           // ETX byte data
    public static byte IDLE = (byte) 0x01;                                          // IDLE byte data
    public static int receiveIndex = 0;                                             // Receive Index

    /* Sensor Data */
    public static int lightValue = 0;
    public static int irValue = 0;
    public static int humidityValue = 0;
    public static int celsiusValue = 0; 
    

    void Start() {
        sp.Open();                              // Open the Serial Port
        sp.ReadTimeout = 500;

        foreach(string str in SerialPort.GetPortNames()){
            //Debug.Log(string.Format("Existing COM port : {0}", str));
        }

        // Create the thread for Serial Data Receive
        spThread = new Thread(spDataReceived);
        spThread.Start();
    }

    // Update is called once per frame
    void Update() {
        
    }

    public static int getReceiveData(){
        int readValue = 0;

        for(int i = 0; i < bufferIndex; i++){
            
            readValue = (int) ((readValue << 8) & buffer[i]);
        }

        return readValue;
    }

    public void spDataReceived(){
        while(true){
            try{
                byte message = (byte) sp.ReadChar();
                //Debug.Log(string.Format("STX Format : {0}", STX));
                //Debug.Log(string.Format("Existing Message from SerialPort : {0}", message));

                if(message == STX){
                    // Clear the received Data
                    receiveIndex = 0;
                    buffer = new byte[bufferSize];
                    bufferIndex = 0;
                } else if((message == IDLE) || (message == ETX)){
                    // Finish the receive sensor data (only one sensor)
                    int receiveData = getReceiveData();

                    //Debug.Log(string.Format("ReceivedData : {0}", receiveData));

                    /*
                    if(receiveIndex == 0)           lightValue = receiveData;
                    else if(receiveData == 1)       irValue = receiveData;
                    else if(receiveData == 2)       humidityValue = receiveData;
                    else if(receiveData == 3)       celsiusValue = receiveData;
                    */

                    // Clear the buffer
                    buffer = new byte[bufferSize];
                    bufferIndex = 0;

                    receiveIndex++;
                } else{
                    
                    buffer[bufferIndex] = message;
                    Debug.Log(string.Format("ReceivedData : {0}", buffer[bufferIndex]));
                    bufferIndex = (bufferIndex + 1) % bufferSize;
                }

            } catch(System.TimeoutException e){                     // Error for Serial Timeout
                print(e);
                sp.Close();
                spThread.Abort();
                throw;
            } catch(System.InvalidOperationException e){            // Error for closed Serial Port
                print(e);
                spThread.Abort();
                throw;
            } catch(System.Exception e){                            // Exception for don't have receiveData
                print(e);
            } 
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
