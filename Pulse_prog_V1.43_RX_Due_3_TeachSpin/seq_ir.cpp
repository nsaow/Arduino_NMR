/* TIME PARAMETERS
 * tecnical : Tsetup, TtrigWidth, Tdeadtime
 * ADC : FID_samples
 * send data to PC : T_transferF 
 * user assigned : Ttr, Tp180, Tp90, TreadoutF, IRtau, nir
 * computed : Tres 
*/
#include <arduino.h>
#include "_PARAM.h"
#include "_SEND2PC.h"
#include "_DUE_ADC.h"
#include "_PINS.h"
#include "_TIMER.h"
#include "seq_ir.h"

double IRtau = 2000;      // 2 ms
uint16_t nIR = 20;        // number of step

int16_t compileIR()
{
// 1. compute time parameters **********
  T_transferF = get_transfer_time(1, FID_samples);
  Tres = Ttr - Tsetup- TtrigWidth - Tp180/2 - IRtau - Tp90/2 - Tdeadtime - TreadoutF - T_transferF;  // us

// 2. Setup ADC  **********
  int er = DUE_ADC_setup(TreadoutF, 1, FID_samples); 
  if ( er > 0)     // error 
    return(er);    
   
// 3. construct events array **********
  int i,j;
  for (i = 0; i < nIR; i++)
  {
    j  = i * 9;   // 9 events per cycles
//  setEvent(ID, PortC_State(Trigger, I, Q, Read, transfer) , rc, nextID);
    setEvent(j+0, PortC_State(0,0,0,1,0), Tsetup, j+1);  
    setEvent(j+1, PortC_State(1,0,0,1,0), TtrigWidth, j+2);  
    setEvent(j+2, PortC_State(0,1,0,1,0), Tp180, j+3);  
    setEvent(j+3, PortC_State(0,0,0,1,0), IRtau - Tp180/2 - Tp90/2 + (double)i*IRtau, j+4);  
    setEvent(j+4, PortC_State(0,1,0,1,0), Tp90, j+5);      
    setEvent(j+5, PortC_State(0,0,0,1,0), Tdeadtime, j+6);  
    setEvent(j+6, PortC_State(0,0,0,0,0), TreadoutF, j+7);  
    setEvent(j+7, PortC_State(0,0,0,1,1), T_transferF, j+8);  
    setEvent(j+8, PortC_State(0,0,0,1,0), Tres - (double)i*IRtau, j+9); 
  }
  setEvent(j+9, PortC_State(0,0,0,0,0), Tsetup, runMode); 
  return(0);      // success
}
