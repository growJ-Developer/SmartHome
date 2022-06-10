#include "DigitalOut.h"
#include "Thread.h"
#include "mbed.h"
#include "Dht11.h"
#include "TextLCD.h"
#include "Servo.h"
#include <string>

#define LCD_ADDRESS_LCD 0x4E // change this according to ur setup

Timer timer;
Ticker flipper;

/* Variable for Serial Communication */
Serial pc(USBTX, USBRX, 115200);
const char STX = 0x02;
const char DLE = 0x01;
const char ETX = 0x03;
const char ACK = 0x06;

/* Sensor Define */
AnalogIn LIGHT_SENSOR(A0);
AnalogIn IR_SENSOR(A1);
Dht11 TEMP_SEONSOR(A2);
I2C i2c_lcd(D14, D15);

/* Variable for Sensor */
double SENSOR_READ_TERM = 0.02;                         // Set Hz(50Hz = 20ms)
int writeValue = 0;
int irValue = 0;
int humidityValue = 0;
int celsiusValue = 0;

/* Output Device Define */
TextLCD_I2C lcd(&i2c_lcd, LCD_ADDRESS_LCD, TextLCD::LCD16x2);
PwmOut LedR(D3);
PwmOut LedG(D5);
PwmOut LedB(D6);

DigitalOut rgb1(D7);            // AC Fan Step
DigitalOut rgb2(D8);            // AC Fan Step
Servo blind(D2);
DigitalOut light(D4);
DigitalOut sink(D9);

/* Variable for Aircondition */
int tempLevel = 1;
float ledStep[] = {0.2, 0.35, 0.5, 0.75, 1.0};
int powerLevel = 0;
int powerStepL[] = {1, 1, 0};
int powerStepR[] = {1, 0, 0};
int isBlind = 0;
int isLight = 0;
int isAC = 0;
int isTV = 0;
int isSink = 0;

/* Function Pre-Declation */
void sendSensorData();
void controlDevice(char Device, char Function, char Instruction);
void controlAC(char Function, char Instruction);
void controlBlind(char Function, char Instruction);
void controlLight(char Function, char Instruction);
void controlTV(char Function, char Instruction);
void controlSink(char Function, char Instruction);
void sinkControl();
void setAcPower();
void setAcTemp();

const int bufferSize = 1024;
char buffer[bufferSize];
int bufferIndex = 0;

int main() {       
    /* Initialize the Aircondition */
    LedR.period(0.005);
    LedB.period(0.005);
    setAcTemp();
    setAcPower();

    while (true) {
        char data = pc.getc();

        if(data == ACK){
            sendSensorData();
        } else if(data == STX){
            memset(buffer, 0, sizeof(buffer));
            bufferIndex = 0;
        } else if(data == ETX){
            controlDevice(buffer[0], buffer[1], buffer[2]);
        } else if(data == DLE)      bufferIndex++;
        else                        buffer[bufferIndex] = data;     
    }
}

void sinkControl(){
    irValue = IR_SENSOR.read_u16();
    if(irValue < 30000) {
        sink = 1;
        isSink = 1;
    } else {
        sink = 0;
        isSink = 0;
    }
}

/* Send the Sensor Data using RX-TX */
void sendSensorData(){
    lcd.cls();
    TEMP_SEONSOR.read();
    sinkControl();
    pc.printf("%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d\r\n", LIGHT_SENSOR.read_u16(), IR_SENSOR.read_u16(), TEMP_SEONSOR.getHumidity(), TEMP_SEONSOR.getCelsius(), tempLevel, isBlind, isLight, isAC, isTV, powerLevel, isSink);
}

/* Control the Device From Serial Data */
void controlDevice(char Device, char Function, char Instruction){
    lcd.cls();
    /* Call the Control Device Function */
    if(Device == 0x21)      controlTV(Function, Instruction);               // Control the TV
    if(Device == 0x22)      controlSink(Function, Instruction);             // Control the Sink
    if(Device == 0x23)      controlAC(Function, Instruction);               // Control the aircondition
    if(Device == 0x24)      controlBlind(Function, Instruction);            // Control the blind
    if(Device == 0x25)      controlLight(Function,  Instruction);           // Control the light(mood)
}

void controlTV(char Function, char Instruction){
    if(Function == 0x41){
        if(Instruction == 0x61){
            lcd.setBacklight(TextLCD::LightOff);
            isTV = 0;
        }
        if(Instruction == 0x62){
            lcd.setBacklight(TextLCD::LightOn);
            isTV = 1;
        }
    }
}

/* Controller for Aircondition */
void controlAC(char Function, char Instruction){
    if(Function == 0x41){
        /* Controll the Power for AC */
        if(Instruction == 0x61) {
            powerLevel = 0;
            isAC = 0;
        }
        if(Instruction == 0x62) {
            powerLevel = 1;
            isAC = 1;
        }    
        setAcPower();
    }
    if(Function == 0x42){
        /* Controll the FAN for AC*/
        if(Instruction == 0x61)     powerLevel = (powerLevel - 1) < 1 ? 1 : powerLevel - 1;
        if(Instruction == 0x62)     powerLevel = (powerLevel + 1) > 2 ? 2 : powerLevel + 1;
        setAcPower();
    }
    if(Function == 0x43){
        /* Controll the Temperature */
        if(Instruction == 0x61)     tempLevel = (tempLevel - 1) < 0 ? 0 : tempLevel - 1;
        if(Instruction == 0x62)     tempLevel = (tempLevel + 1) > 4 ? 4 : tempLevel + 1;
        setAcTemp();   
    }
}

void setAcPower() {
    rgb1.write(powerStepL[powerLevel]);
    rgb2.write(powerStepR[powerLevel]);
}

void setAcTemp() {
    LedR.write(ledStep[tempLevel]);
    LedB.write(ledStep[4 - tempLevel]);
}

/* Controller for Blind */
void controlBlind(char Function, char Instruction){
    if(Function == 0x41){
        if(Instruction == 0x61){
            blind.write(180);
            isBlind = 0;
        } else if(Instruction == 0x62){
            blind.write(0);
            isBlind = 1;
        }
    }
}

/* Controller for Light */
void controlLight(char Function, char Instruction){
    if(Function == 0x41){
        if(Instruction == 0x61){
            light = 0;
            isLight = 0;
        } else if(Instruction == 0x62){
            light = 1;
            isLight = 1;
        }
    }
}

void controlSink(char Function, char Instruction){
    if(Function == 0x41){
        if(Instruction == 0x61){
            sink = 0;
            isSink = 0;
        }
        if(Instruction == 0x62){
            sink = 1;
            isSink = 1;
        }
    }
}
