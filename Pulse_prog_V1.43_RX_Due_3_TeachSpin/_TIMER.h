#include <Arduino.h>

#ifndef _TIMER_H_
#define _TIMER_H_

struct pulseEvents {
  uint32_t state;       // pins state
  int32_t rc;           // counting number
  int32_t nextID;       // Next event
};
/********************* Serial data transfer *****************************/
const uint16_t max_events = 4096;
extern uint16_t Signal_samples;               
extern double Signal_transfer_time;         
extern double Treadout;
extern pulseEvents events[max_events]; 
extern int32_t eventID;
extern int nAvg;           

/***** subroutines *********************************************************************************************/
void TimerInit(Tc *tc, uint32_t channel, IRQn_Type irq, uint32_t tcCLK );
void setEvent(int32_t ID, uint32_t state, double hold_us, int32_t nextID);
void startSeq();
void stopSeq();
#endif
