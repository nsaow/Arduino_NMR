#include <arduino.h>

#ifndef SEQ_CPMG_L_H_
#define SEQ_CPMG_L_H_

//******************** pulse sequence variable *******************//
extern double Tte;
extern uint16_t ne;

//******************** pulse sequence sub routine *******************//
int16_t compileCPMG();
#endif
