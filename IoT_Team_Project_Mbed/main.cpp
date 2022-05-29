#include "AnalogIn.h"
#include "Ticker.h"
#include "Timer.h"
#include "mbed.h"
#include "mbed_wait_api.h"
#include "platform/mbed_thread.h"
#include <Dht11.h>
#include <RS485.h>

AnalogIn LIGHT_SENSOR(A0);
AnalogIn IR_SENSOR(A1);
Dht11 TEMP_SEONSOR(A2);

Timer timer;
Ticker flipper;

Serial pc(SERIAL_TX, SERIAL_RX, 115200);

const char STX = '\2';
const char ETX = '\3';
const char IDLE = '\1';

double SENSOR_READ_TERM = 0.5;
bool isRead = false;

void readSensor() { isRead = true;} 
void sendIntegerData(int value){ for(int i = 0; i < sizeof(int); i++)    pc.printf("%c", *(((char *) &value) + i)); }

/* Send the Sensor Data using RX-TX */
void sendSensorData(){
    pc.printf("%c", STX);                               // Send the STX
    sendIntegerData(LIGHT_SENSOR.read_u16());           // Send the Light Data
    pc.printf("%c", IDLE);    
    sendIntegerData(IR_SENSOR.read_u16());              // Send the IR Data
    pc.printf("%c", IDLE);                              
    TEMP_SEONSOR.read();
    sendIntegerData(TEMP_SEONSOR.getHumidity());        // Send the Humidity Data
    pc.printf("%c", IDLE);                              
    sendIntegerData(TEMP_SEONSOR.getCelsius());         // Send the Celsius Data
    pc.printf("%c", ETX);                               // Send the ETX
}

int main()
{
    flipper.attach(&readSensor, SENSOR_READ_TERM);

    int readIndex = 0;
    char buffer[sizeof(int)];
    

    while (true) {
        if(pc.readable()){
            char byte = pc.getc();        
            if(byte == STX){
                // Logic Code for STX
            } else if(byte == ETX){
                // Logic Code for IDLE
            } else if(byte == ETX){
                // Logic Code for ETX
                // Control Code.... and buffer to Integer
            } else  buffer[readIndex % sizeof(int)] = byte;
        }

        /* If read the sensor data, send to other device */
        if(isRead){
            sendSensorData();
            isRead = false;
        }
    }
}
