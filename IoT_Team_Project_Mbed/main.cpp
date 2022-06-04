#include "Dht11.h"
#include "DigitalOut.h"
#include "Serial.h"
#include "mbed.h"
#include "platform/mbed_thread.h"
#include "TextLCD.h"
#include <string>

#define LCD_ADDRESS_LCD 0x4E // change this according to ur setup

DigitalOut led1(LED1);
DigitalOut led2(LED2);

AnalogIn LIGHT_SENSOR(A0);
AnalogIn IR_SENSOR(A1);
Dht11 TEMP_SEONSOR(A2);
I2C i2c_lcd(D14, D15);
TextLCD_I2C lcd(&i2c_lcd, LCD_ADDRESS_LCD, TextLCD::LCD16x2);

Timer timer;
Ticker flipper;

Serial pc(USBTX, USBRX, 115200);

const char STX = 0x02;
const char DLE = 0x01;
const char ETX = 0x03;
const char ACK = 0x06;

double SENSOR_READ_TERM = 0.02;                         // Set Hz(50Hz = 20ms)
bool isRead = false;
bool isSend = true;

int writeValue = 0;
int irValue = 0;
int humidityValue = 0;
int celsiusValue = 0;


void readSensor() { isRead = true;} 
void sendSensorData();
void receiveData();
void controlDevice(char Device, char Function, char Instruction);

char buffer[128];
int bufferIndex = 0;

int main()
{    
    lcd.setBacklight(TextLCD::LightOn);
    flipper.attach(&readSensor, SENSOR_READ_TERM);

    while (true) {
        char data = pc.getc();

        if(data == ACK){
            lcd.cls();
            lcd.printf("Req Sensor");
            sendSensorData();
        } else if(data == STX){
            memset(buffer, 0, sizeof(buffer));
            bufferIndex = 0;
        } else if(data == ETX){
            controlDevice(buffer[0], buffer[1], buffer[2]);
        } else if(data == DLE){
            bufferIndex++;
        } else {
            buffer[bufferIndex] = data;     
        }        
    }
}

/* Get Sensor Data */
void checkSensorData(){
    TEMP_SEONSOR.read();
}

/* Send the Sensor Data using RX-TX */
void sendSensorData(){
    pc.printf("%d,%d,%d,%d\r\n", LIGHT_SENSOR.read_u16(), IR_SENSOR.read_u16(), TEMP_SEONSOR.getHumidity(), TEMP_SEONSOR.getCelsius());
}

/* Control the Device From Serial Data */
void controlDevice(char Device, char Function, char Instruction){
    lcd.cls();

    if(Device == 0x21) {
        lcd.printf("TV, ");
        if(Function == 0x41){
            lcd.printf("POWER, ");
            if(Instruction == 0x61){
                lcd.printf("ON");
            }
        }
    } else if(Device == 0x22) {
        lcd.printf("SPEAKER, ");
        if(Function == 0x42){
            lcd.printf("VOLUME, ");
            if(Instruction == 0x62){
                lcd.printf("UP");
            }
        }
    } 

    /* Call the Control Device Function */
}
