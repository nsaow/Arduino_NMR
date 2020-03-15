/* TIME PARAMETERS
 * tecnical : Tsetup, TtrigWidth, Tdeadtime
 * ADC : FID_samples
 * send data to PC : T_transferF 
 * user assigned : Ttr, Tp90, TreadoutF
 * computed : Tres 
*/
#include <arduino.h>
#include "_PARAM.h"
#include "_SEND2PC.h"
#include "_DUE_ADC.h"
#include "_PINS.h"
#include "_TIMER.h"
#include "seq_tuning.h"

int16_t compileTuning()
{
// 1. compute time parameters **********
  T_transferF = get_transfer_time(1, FID_samples);
  Tres = Ttr - Tsetup- TtrigWidth - Tp90 - Tdeadtime - TreadoutF - T_transferF;  // us

// 2. Setup ADC  **********
  int er = DUE_ADC_setup(TreadoutF, 1, FID_samples); 
  if ( er > 0)     // error 
    return(er);   
   
// 3. construct events array **********
//setEvent(ID, PortC_State(Trigger, I, Q, Read, transfer) , rc, nextID);
  setEvent(0,PortC_State(0,0,0,1,0), Tsetup, 1); 
  setEvent(1,PortC_State(1,0,0,1,0), TtrigWidth, 2);
  setEvent(2,PortC_State(0,1,0,1,0), Tp90, 3); 
  setEvent(3,PortC_State(0,0,0,1,0), Tdeadtime, 4); 
  setEvent(4,PortC_State(0,0,0,0,0), TreadoutF, 5); 
  setEvent(5,PortC_State(0,0,0,1,1), T_transferF, 6); 
  setEvent(6,PortC_State(0,0,0,1,0), Tres, runMode);
  return(0);      // success
}
