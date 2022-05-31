using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

/* How To Check the Serial Port?
/*  If use the mac, turn on the Terminal.
/*   wirte the command 'ls /dev/tty.*
/*   then, you can see like '/dev/tty.usbmodem1403'
/*
/*  If use the windows, search on device manager
*/


public class IoTController : MonoBehaviour {
    SerialPort sp = new SerialPort("/dev/tty.usbmodem1203", 115200, Parity.None, 8, StopBits.None);     // Open Serial Port(Mac)
    //SerialPort sp = new SerialPort("COM4", 115200, Parity.None, 8, StopBits.None);                      // Open Serial Port(Windows)
    
    public static byte[] buffer = new byte[sizeof(int)];                // Byte Array for receive Serial Data
    public static char STX = (char)2;                                   // STX byte data
    public static char ETX = (char)3;                                   // ETX byte data
    public static char IDLE = (char)1;                                  // IDLE byte data
    public static int recv_index = 0;                                   // Receive Index
    

    void Start() {
        sp.Open();                              // Open the Serial Port
        sp.ReadTimeout = 100;

        foreach(string str in SerialPort.GetPortNames()){
            Debug.Log(string.Format("Existing COM port : {0}", str));
        }
    }

    // Update is called once per frame
    void Update() {
        
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
