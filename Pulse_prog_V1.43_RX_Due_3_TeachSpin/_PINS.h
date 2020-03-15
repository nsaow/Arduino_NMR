#include <arduino.h>

#ifndef _PINS_H_
#define _PINS_H_

/******************** variables ********************/

/******************** subroutines ********************/
void pulsePinsInit(uint8_t pTrig, uint8_t pI, uint8_t pQ, uint8_t pRd, uint8_t ptx);
uint32_t PortC_State(uint32_t Trig_st, uint32_t I_st, uint32_t Q_st, uint32_t Re_st, uint32_t tx_st);
#endif
