/* TIME PARAMETERS
 * tecnical : Tsetup, TtrigWidth
 * ADC : FID_samples
 * send data to PC : T_transferE
 * user assigned : Ttr, Tp90, Tp180, Tte, ne
 * computed : TreadoutE, Tres 
*/
#include <arduino.h>
#include "_PARAM.h"
#include "_SEND2PC.h"
#include "_DUE_ADC.h"
#include "_PINS.h"
#include "_TIMER.h"
#include "seq_cpmg.h"

double Tte = 2000;        // 2 ms
uint16_t ne = 10;         // number of echoes

//******************** pulse sequence sub routine *******************//
int16_t compileCPMG()
{
// 1. compute time parameters **********
  T_transferE = get_transfer_time(ne, Echo_samples);  // transfer all echoes at once after sequence finished
  TreadoutE = Tte/2;                                      // readout time = half echo time
  Tres = Ttr - Tsetup- TtrigWidth - Tp90/2 - Tte/2 + Tp180/2 - (double)ne*Tte - T_transferE;  // us

// 2. Setup ADC  **********
  int er = DUE_ADC_setup(TreadoutE, ne, Echo_samples); 
  if ( er > 0)     // error 
    return(er);    
   
// 3. construct events array **********
  int i,j;
//setEvent(ID, PortC_State(Trigger, I, Q, Read, transfer) , rc, nextID);  
  setEvent(0, PortC_State(0,0,0,1,0), Tsetup, 1);
  setEvent(1, PortC_State(1,0,0,1,0), TtrigWidth, 2);
  setEvent(2, PortC_State(0,1,0,1,0), Tp90, 3);
  setEvent(3, PortC_State(0,0,0,1,0), Tte/2 - Tp90/2 - Tp180/2, 4);
  for (i = 0; i < ne; i++)
  {
    j  = i * 4;   // 4 events per cycles
    setEvent(j+4, PortC_State(0,0,1,1,0), Tp180, j+5);
    setEvent(j+5, PortC_State(0,0,0,1,0), Tte/4 - Tp180/2, j+6);
    setEvent(j+6, PortC_State(0,0,0,0,0), TreadoutE, j+7);
    setEvent(j+7, PortC_State(0,0,0,1,0), Tte/4 - Tp180/2, j+8);
  }
  setEvent(j+8, PortC_State(0,0,0,1,1), T_transferE, j+9);
  setEvent(j+9, PortC_State(0,0,0,1,0), Tres, runMode);
  return(0);      // success
}
