#include "I2C.h"
#include "TextLCD.h"
#include "Dht11.h"
#include "mbed.h"
#include "platform/mbed_thread.h"

AnalogIn LIGHT_SENSOR(A0);
AnalogIn IR_SENSOR(A1);
Dht11 TEMP_SEONSOR(A2);

I2C i2c(D14, D15);
TextLCD_I2C lcd(&i2c, 0x4E, TextLCD::LCD16x2);

Timer timer;
Ticker flipper;

//Serial pc(SERIAL_TX, SERIAL_RX, 115200);
Serial pc(USBTX, USBRX, 115200);

const char STX = 0x02;
const char ETX = 0x03;
const char DLE = 0x01;

double SENSOR_READ_TERM = 0.5;                         // Set Hz(50Hz = 20ms)
bool isRead = false;

void readSensor() { isRead = true;} 

/* Send the Sensor Data using RX-TX */
void sendSensorData(){
    TEMP_SEONSOR.read();
    if(pc.writeable())      pc.printf("%d,%d,%d,%d\r\n", LIGHT_SENSOR.read_u16(), IR_SENSOR.read_u16(), TEMP_SEONSOR.getHumidity(), TEMP_SEONSOR.getCelsius());
    isRead = true;
}

// Test LED
Thread tThread;
int led_interval = 1000;
bool flag = false;
void led_thread(){
    DigitalOut led(LED1);
    while(true){
        led = flag;
        flag = !flag;
        wait_ms(led_interval);
    }
}

/* Control the Device base of Recieved Data */
void controlDevice(char Device, char Function, char Instruction){
    // Compare Error...
    if(Device == (char) 0x21){
        led_interval = 100;
    } else if(Device == (char) 0x22){
        led_interval = 50;
    }
}

int main()
{
    // LCD Error...
    lcd.cls();
    lcd.setBacklight(TextLCD::LightOn);
    //lcd.printf("LCD Hello\n\r");

    flipper.attach(&readSensor, SENSOR_READ_TERM);

    tThread.start(led_thread);   

    int readIndex = 0;
    char recievedData[3] = {0x00, 0x00, 0x00};

    while (true) {
        lcd.printf("LCD Hello\n\r");


        if(pc.readable()){
            char byte = pc.getc();   

            if(byte == STX)         readIndex = 0;
            else if(byte == ETX)    controlDevice(recievedData[0], recievedData[1], recievedData[2]);
            else if(byte == DLE)    readIndex = readIndex + 1;
            else                    recievedData[readIndex] = (char) byte;
        }

        /* If read the sensor data, send to other device */
        if(isRead){
            sendSensorData();
            isRead = false;
        }
    }
}

