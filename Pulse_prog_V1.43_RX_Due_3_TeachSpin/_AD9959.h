  /*
 * pins connection (Arduino Due >> AD9959)
 * P0_PIN >> P0                   not used
 * P1_PIN >> P1                   not used
 * P2_PIN >> P2                   not used
 * P3_PIN >> P3                   not used
 * IO_UPDATE_PIN >> I/O_UPDATE    A rising edge transfers data from the serial I/O port buffer to active registers.
 * CS_PIN >> CS                   CHip select active low.
 * SDIO1_PIN >> SDIO_1            not used
 * SDIO2_PIN >> SDIO_2            not used
 * SDIO3_PIN >> SDIO_3            not used 
 * RESET_PIN >> RESET             Active High Reset Pin.
 * P_DOWN_PIN >> P_DOWN           not used
 * SIP pins  
 * SCLK >> SCLK                   Serial Data Clock for I/O Operations
 * MOSI >> SDIO_0                 Data Pin SDIO_0 is dedicated to the serial port I/O only.
 * 
 * !!Important!! All unused pins must be connected to Ground or set logic LOW.
 */
 
#include <Arduino.h>

#ifndef _AD9959_H_
#define _AD9959_H_

extern uint8_t P0_PIN;
extern uint8_t P1_PIN;
extern uint8_t P2_PIN;
extern uint8_t P3_PIN;
extern uint8_t IO_UPDATE_PIN;
extern uint8_t CS_PIN;
extern uint8_t SDIO1_PIN;
extern uint8_t SDIO2_PIN;
extern uint8_t SDIO3_PIN;
extern uint8_t RESET_PIN;
extern uint8_t P_DOWN_PIN;
extern const uint8_t AD9959_CH[4];

/********** subroutines **********/
void AD9959_Init();                                     // initailize Pins SPI and set default values
void AD9959_SetFreq(uint8_t CH_bits,double freq);       // frequency 0 to 200 MHz
void AD9959_SetPhase(uint8_t CH_bits,double phase);     // phase 0 to 360 degrees
void AD9959_SetAmp(uint8_t CH_bits,unsigned long amp);  // amplitude 0 to 1023 

#endif
