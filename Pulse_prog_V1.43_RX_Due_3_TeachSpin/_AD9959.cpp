/*
 * pins connection (Arduino Due >> AD9959)
 * P0_PIN >> P0                   connect but not used
 * P1_PIN >> P1                   connect but not used
 * P2_PIN >> P2                   connect but not used
 * P3_PIN >> P3                   connect but not used
 * IO_UPDATE_PIN >> I/O_UPDATE    A rising edge transfers data from the serial I/O port buffer to active registers.
 * CS_PIN >> CS                   CHip select active low.
 * SDIO1_PIN >> SDIO_1            connect but not used
 * SDIO2_PIN >> SDIO_2            connect but not used
 * SDIO3_PIN >> SDIO_3            connect but not used 
 * RESET_PIN >> RESET             Active High Reset Pin.
 * P_DOWN_PIN >> P_DOWN           connect but not used
 * SIP pins  
 * SCLK >> SCLK                   Serial Data Clock for I/O Operations
 * MOSI >> SDIO_0                 Data Pin SDIO_0 is dedicated to the serial port I/O only.
 * 
 * !!Important!! All unused pins must be Grounded or set to logic LOW.
 * example:: frequency, phase, amplitude setting
 *   void AD9959_Init();
 *   AD9959_SetFreq(0x30,20000000);  // CH0 and CH1 freq = 20 MHz
 *   AD9959_SetPhase(0x20,180);      // CH1 phase = 180 degree
 *   AD9959_SetAmp(0x20,512);        // CH1 Amplitude 50% (512 of max 1023)
 */
 
#include <Arduino.h>
#include <SPI.h>
#include "_AD9959.h"

//pins default , the pins must changed to match your real connection
 uint8_t P0_PIN = 2;
 uint8_t P1_PIN = 3;
 uint8_t P2_PIN = 4;
 uint8_t P3_PIN = 5;
 uint8_t IO_UPDATE_PIN = 6;
 uint8_t CS_PIN = 7;
 uint8_t SDIO1_PIN = 8;
 uint8_t SDIO2_PIN = 9;
 uint8_t SDIO3_PIN = 10;
 uint8_t RESET_PIN = 11;
 uint8_t P_DOWN_PIN = 12;
 
// register addresses
const uint8_t CSR_addr = 0x00;    // Channel Select Register (1 byte)
const uint8_t FR1_addr = 0x01;    // Function Register 1 (3 bytes)
const uint8_t FR2_addr = 0x02;    // Function Register 2 (2 bytes)
const uint8_t CFR_addr = 0x03;    // Channel Function Register (3 bytes)
const uint8_t CFTW0_addr = 0x04;  // Channel Frequency Tuning Word 0 (4 bytes) 
const uint8_t CPOW0_addr = 0x05;  // Channel Phase Offset Word 0 (2 bytes)
const uint8_t ACR_addr = 0x06;    // Amplitude Control Register (3 bytes)

// register size
const int CSR_size = 1;    // Channel Select Register (1 byte)
const int FR1_size = 3;    // Function Register 1 (3 bytes)
const int FR2_size = 2;    // Function Register 2 (2 bytes)
const int CFR_size = 3;    // Channel Function Register (3 bytes)
const int CFTW0_size = 4;  // Channel Frequency Tuning Word 0 (4 bytes) 
const int CPOW0_size = 2;  // Channel Phase Offset Word 0 (2 bytes)
const int ACR_size = 3;    // Amplitude Control Register (3 bytes)

// register data variables
unsigned long CSR;
unsigned long FR1;
unsigned long FR2;
unsigned long CFR[4];
unsigned long CFTW0[4];
unsigned long CPOW0[4];
unsigned long ACR[4];

// board design clock
double AD9959_sys_clk = 25000000;
double AD9959_clk_mul = 20;
double AD9959_freq_to_FTW = 4294967296.0 / AD9959_sys_clk / AD9959_clk_mul;
double AD9959_phase_to_POW = 16384.0 / 360.0;

// Channel selection byte
const uint8_t AD9959_CH[4] = {0x10, 0x20, 0x40, 0x80};

/********** subroutines **********/
/*
CH_bits value           Selected channels
Binary    hexadecimal   Ch0 Ch1 Ch2 Ch3
0000 0000   0x00        No  No  No  No
0001 0000   0x10        Yes No  No  No
0010 0000   0x20        No  Yes No  No
0011 0000   0x30        Yes Yes No  No
0100 0000   0x40        No  No  Yes No
0101 0000   0x50        Yes No  Yes No
0110 0000   0x60        No  Yes Yes No
0111 0000   0x70        Yes Yes Yes No
1000 0000   0x80        No  No  No  Yes
1001 0000   0x90        Yes No  No  Yes
1010 0000   0xA0        No  Yes No  Yes
1011 0000   0xB0        Yes Yes No  Yes
1100 0000   0xC0        No  No  Yes Yes
1101 0000   0xD0        Yes No  Yes Yes
1110 0000   0xE0        No  Yes Yes Yes
1111 0000   0xF0        Yes Yes Yes Yes
*/

void default_Reg_data(){
  CSR=0xF0;
  FR1=0x00D00000;
  FR2=0x00000000;
  CFR[0]=0x00000304; 
  CFR[1]=0x00000304; 
  CFR[2]=0x00000304; 
  CFR[3]=0x00000304; 
  CFTW0[0]=0x0083126E;    // 1 MHz   
  CFTW0[1]=0x0083126E;    // 1 MHz   
  CFTW0[2]=0x0083126E;    // 1 MHz   
  CFTW0[3]=0x0083126E;    // 1 MHz   
  CPOW0[0]=0x00000000;    // phase 0
  CPOW0[1]=0x00001000;    // phase 90
  CPOW0[2]=0x00000000;    // phase 180
  CPOW0[3]=0x00001000;    // phase 270
  ACR[0]=0x000013FF;      // Amplitude 1023 = max 
  ACR[1]=0x000013FF;      // Amplitude 1023 = max
  ACR[2]=0x000013FF;      // Amplitude 1023 = max
  ACR[3]=0x000013FF;      // Amplitude 1023 = max
}

void AD9959_write(uint8_t address, unsigned long data, int RegSize){
  int i;
  uint8_t value[4];
  for (i=0;i<RegSize;i++){
    value[i] = data >> (i*8) & 0xFF; 
  }
  digitalWrite(CS_PIN,LOW);
  SPI.transfer(address);
  for (i=RegSize-1;i>-1;i--)
    SPI.transfer(value[i]);
  digitalWrite(CS_PIN,HIGH);
  delayMicroseconds(4);
}

void AD9959_Reset(){
  digitalWrite(RESET_PIN,HIGH);
  delayMicroseconds(2);     
  digitalWrite(RESET_PIN,LOW);       
  delayMicroseconds(10);
}

void AD9959_IO_Update(){
  digitalWrite(IO_UPDATE_PIN,LOW);
  digitalWrite(IO_UPDATE_PIN,HIGH);
  digitalWrite(IO_UPDATE_PIN,LOW);
  delayMicroseconds(10);
}

void AD9959_SetFreq(uint8_t CH_bits,double freq){
  unsigned long FTW;
  FTW = (unsigned long) (freq * AD9959_freq_to_FTW);
  AD9959_write(CSR_addr,CH_bits,CSR_size);        // enable the selected channels
  AD9959_write(CFTW0_addr,FTW,CFTW0_size);        // write frequency
  AD9959_IO_Update();
}

void AD9959_SetPhase(uint8_t CH_bits,double phase){
  unsigned long POW;
  POW = (unsigned long) (phase * AD9959_phase_to_POW);
  AD9959_write(CSR_addr,CH_bits,CSR_size);        // enable the selected channels
  AD9959_write(CPOW0_addr,POW,CPOW0_size);        // write phase
  AD9959_IO_Update();
}

void AD9959_SetAmp(uint8_t CH_bits,unsigned long amp){
  CSR = CH_bits;
  AD9959_write(CSR_addr,CH_bits,CSR_size);        // enable the selected channels
  AD9959_write(ACR_addr,amp+0x1000,ACR_size);     // write phase
  AD9959_IO_Update();
}

void AD9959_Init()
{
  // pins initailization
  pinMode(P0_PIN,OUTPUT);
  pinMode(P1_PIN,OUTPUT);
  pinMode(P2_PIN,OUTPUT);
  pinMode(P3_PIN,OUTPUT);
  pinMode(IO_UPDATE_PIN,OUTPUT);
  pinMode(CS_PIN,OUTPUT);
  pinMode(SDIO1_PIN,OUTPUT);
  pinMode(SDIO2_PIN,OUTPUT);
  pinMode(SDIO3_PIN,OUTPUT);
  pinMode(RESET_PIN,OUTPUT);
  pinMode(P_DOWN_PIN,OUTPUT);
  digitalWrite(P0_PIN,LOW);           // not used
  digitalWrite(P1_PIN,LOW);           // not used
  digitalWrite(P2_PIN,LOW);           // not used
  digitalWrite(P3_PIN,LOW);           // not used
  digitalWrite(IO_UPDATE_PIN,LOW);    // Default LOW
  digitalWrite(CS_PIN,HIGH);          // Default HIGH
  digitalWrite(SDIO1_PIN,LOW);        // not used
  digitalWrite(SDIO2_PIN,LOW);        // not used
  digitalWrite(SDIO3_PIN,LOW);        // not used
  digitalWrite(RESET_PIN,LOW);        // Default LOW

  // Initial SPI
  SPI.begin();
  SPI.setBitOrder(MSBFIRST);
  SPI.setClockDivider(164);    // reduce speed to 1MHz SCLK
  
  AD9959_Reset();       // Master reset
  default_Reg_data();   // recal default values

  // initialize control registers
  AD9959_write(FR1_addr,FR1,FR1_size);
  AD9959_write(FR2_addr,FR2,FR2_size);
  AD9959_IO_Update();

  /******************** set default frequency = 1 MHz, amplitude = 1023 (max) to All channels ********************/ 
  CSR = 0xF0;                                     // select all channels
  AD9959_write(CSR_addr,CSR,CSR_size);            // enable all channels
  AD9959_write(CFR_addr,CFR[0],CFR_size);         // write CFR
  AD9959_write(CFTW0_addr,CFTW0[0],CFTW0_size);   // write frequency
  AD9959_write(ACR_addr,ACR[0],ACR_size);         // write amplitude
  AD9959_IO_Update();

  // set default phase CH0
  CSR = 0x10;                                     // select channel 0
  AD9959_write(CSR_addr,CSR,CSR_size);            // enable channel 0
  AD9959_write(CPOW0_addr,CPOW0[0],CPOW0_size);   // write phase 0 degree to channel 0
  AD9959_IO_Update();

  // set default phase CH1
  CSR = 0x20;                                     // select channel 1
  AD9959_write(CSR_addr,CSR,CSR_size);            // enable channel 1
  AD9959_write(CPOW0_addr,CPOW0[1],CPOW0_size);   // write phase 90 degree to channel 1
  AD9959_IO_Update();

  // set default phase CH2
  CSR = 0x40;                                     // select channel 2
  AD9959_write(CSR_addr,CSR,CSR_size);            // enable channel 2
  AD9959_write(CPOW0_addr,CPOW0[2],CPOW0_size);   // write phase 180 degree to channel 2
  AD9959_IO_Update();

  // set default phase CH3
  CSR = 0x80;                                     // select channel 3
  AD9959_write(CSR_addr,CSR,CSR_size);            // enable channel 3
  AD9959_write(CPOW0_addr,CPOW0[3],CPOW0_size);   // write phase 270 degree to channel 3
  AD9959_IO_Update();
  delayMicroseconds(10);
}
