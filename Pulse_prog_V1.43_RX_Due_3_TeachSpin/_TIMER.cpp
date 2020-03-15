/*
Due has 5 internal clocks speed available for timer counter
TC_CMR_TCCLKS_TIMER_CLOCK1 :: 84Mhz/2 = 42.000 MHz      
TC_CMR_TCCLKS_TIMER_CLOCK2 :: 84Mhz/8 = 10.500 MHz    stable@ > 14 counts // we use this clock rate
TC_CMR_TCCLKS_TIMER_CLOCK3 :: 84Mhz/32 = 2.625 MHz
TC_CMR_TCCLKS_TIMER_CLOCK4 :: 84Mhz/128 = 656.250 KHz
TC_CMR_TCCLKS_TIMER_CLOCK5 :: SLK = 32 KHz

Due has 3 timer counters,each of them have 3 channels
TC# :channel  : IRQn_Type : irq handle :  PMC ID
TC0   0         TC0_IRQn    TC0_Handle    ID_TC0
TC0   1         TC1_IRQn    TC1_Handle    ID_TC1
TC0   2         TC2_IRQn    TC2_Handle    ID_TC2
TC1   0         TC3_IRQn    TC3_Handle    ID_TC3
TC1   1         TC4_IRQn    TC4_Handle    ID_TC4
TC1   2         TC5_IRQn    TC5_Handle    ID_TC5
TC2   0         TC6_IRQn    TC6_Handle    ID_TC6
TC2   1         TC7_IRQn    TC7_Handle    ID_TC7
TC2   2         TC8_IRQn    TC8_Handle    ID_TC8

using the timer counters (TC)
   1. to start TC
      - TC_SetRC(tc, channel, rc);
      - TC_Start(tc, channel);
   2. to set new counting value
      - TC_GetStatus(tc, channel);      // important
      - TC_SetRC(tc, channel, rc);
   3. to stop counting
      - TC_GetStatus(tc, channel);      // important
      - TC_Stop(tc, channel);

This lib. using Timmer TC2 channel 1 i.e. ID_TC7
This lib. using four pins of "port C" to output sequence signals
*/

#include <Arduino.h>
#include "_TIMER.h"
#include "_PARAM.h"

pulseEvents events[max_events];
int32_t eventID;

/******************** sub-routines ********************/
void TimerInit(Tc *tc, uint32_t channel, IRQn_Type irq, uint32_t tcCLK )
{
   pmc_set_writeprotect(false);
   pmc_enable_periph_clk((uint32_t) irq);
   TC_Configure(tc, channel, TC_CMR_WAVE | TC_CMR_WAVSEL_UP_RC | tcCLK);
   tc->TC_CHANNEL[channel].TC_IER=  TC_IER_CPCS ;  
   tc->TC_CHANNEL[channel].TC_IDR=~(TC_IER_CPCS);
   NVIC_EnableIRQ(irq);
}

uint32_t usToCountCLK2(float t) 
{ 
  return( round(t*10.5000) ); 
}

void setEvent(int32_t ID, uint32_t state, double hold_us, int32_t nextID)
{
  events[ID].state = state;                     // pins state
  events[ID].rc = usToCountCLK2(hold_us);       // convert us to counting number for timer counter
  events[ID].nextID = nextID;                   // the next event
}

void TC7_Handler()
{
   if (eventID < 0)                             //  if data acquisition mode  
   {
      TC_GetStatus(TC2, 1);                     // read status to clear the interrupt
      TC_Stop(TC2,1);                           // stop counting
      nAcq = nAcq + 1;                          // number of acquisition + 1
      if (nAcq < nAvg)                          // need more acquisition
        startSeq();
      else
        seqRunning = false;                     // end acquisition (no sequence running)
   }
   else
   {
     PIOC->PIO_ODSR = events[eventID].state;    // make change port value
     TC_GetStatus(TC2, 1);                      // read status to clear the interrupt
     TC_SetRC(TC2,1,events[eventID].rc);        // set number of counter and start counting
     eventID = events[eventID].nextID;          // index to next event
   }
}

void startSeq()
{
  eventID = 0;
  PIOC->PIO_ODSR = events[eventID].state;       // starting pins status
  TC_SetRC(TC2, 1, events[eventID].rc);         // starting delay time (for counter)
  eventID = events[eventID].nextID;             // the next event
  TC_Start(TC2, 1);                             // start timer counter
}

void stopSeq()
{
  TC_GetStatus(TC2, 1);
  TC_Stop(TC2, 1);                              // stop timmer counter
  PIOC->PIO_ODSR = 0x00000000;                  // all pins of C port are set to LOW
}
