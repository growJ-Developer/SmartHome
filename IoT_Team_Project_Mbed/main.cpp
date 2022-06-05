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

/* Variable for Aircondition */
int tempLevel = 3;
bool acAuto = false;
float ledRed = 0.5;
float ledBlue = 0.5;


bool isRead = false;
bool isSend = true;

/* Function Pre-Declation */
void readSensor() { isRead = true;} 
void sendSensorData();
void receiveData();
void controlDevice(char Device, char Function, char Instruction);
void acPowerControl(char Function, char Instruction);
void acTempControl(char Function, char Instruction);
void setAcPower(float R, float G, float period, int L1, int L2);
void setAcTemp(float period);
void controlBlind(char Function, char Instruction);
void controlLight(char Function, char Instruction);

const int bufferSize = 256;
char buffer[bufferSize];
int bufferIndex = 0;

int main() {       
    lcd.setBacklight(TextLCD::LightOn);

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
        } else if(data == DLE)      bufferIndex++;
        else                        buffer[bufferIndex] = data;     
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
    } 
    if(Device == 0x22) {
        lcd.printf("SPEAKER, ");
        if(Function == 0x42){
            lcd.printf("VOLUME, ");
            if(Instruction == 0x62){
                lcd.printf("UP");
            }
        }
    } 


    /* Aircondition Controller */
    if(Device == 0x23)      acPowerControl(Function, Instruction);
    if(Device == 0x24)      acTempControl(Function, Instruction);

    if(Device == 0x25)      controlBlind(Function, Instruction);
    if(Device == 0x26)      controlLight(Function,  Instruction);

    /* Call the Control Device Function */
}

/* Controller for AirCondition Power */
void acPowerControl(char Function, char Instruction){
    lcd.printf("AC Power");
    if(Function == 0x43)    setAcPower(0.5, 0.5, 0.005, 0, 1);
    if(Function == 0x44)    setAcPower(0.5, 0.5, 0.005, 0, 0);
}

/* Controller for AirCondition Temperature */
void acTempControl(char Function, char Instruction){
    lcd.printf("AC Temp, ");
    if(Function == 0x42){
        if(Instruction == 0x62) {
            lcd.printf("Temp Down");
            if(tempLevel > 1){
                 ledRed -= 0.25;
                 ledBlue += 0.25;
                 tempLevel--;
            }
        }
        if(Instruction == 0x63) {
            lcd.printf("Temp Up");
            if(tempLevel < 5){
                 ledRed += 0.25;
                 ledBlue -= 0.25;
                 tempLevel++;
            }
        }
    }
    if(Function == 0x43) {
        if(tempLevel > 1){
            ledRed -= 0.25;
            ledBlue += 0.25;
            tempLevel--;
        }
    }
    if(Function == 0x44){
        if(tempLevel > 1){
            ledRed += 0.25;
            ledBlue -= 0.25;
            tempLevel++;
        }
    }

    setAcTemp(0.005);
}

void setAcPower(float R, float G, float period, int L1, int L2) {
    LedR.write(R);
    LedB.write(G);
    LedR.period(period);
    LedB.period(period);
    rgb1.write(L1);
    rgb2.write(L2);
}

void setAcTemp(float period) {
    LedR.write(ledRed);
    LedB.write(ledBlue);
    LedR.period(period);
    LedB.period(period);
}

/* Controller for Blind */
void controlBlind(char Function, char Instruction){
    lcd.printf("Blind Control");
    if(Function == 0x41){
        if(Instruction == 0x61){
            blind.write(180);
        } else if(Instruction == 0x62){
            blind.write(0);
        }
    }
}

/* Controller for Light */
void controlLight(char Function, char Instruction){
    lcd.printf("Light Control");
    if(Function == 0x41){
        if(Instruction == 0x61){
            light = 1;
        } else if(Instruction == 0x62){
            light = 0;
        }
    }
}
