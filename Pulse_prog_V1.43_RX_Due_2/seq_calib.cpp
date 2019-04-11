/* TIME PARAMETERS
 * tecnical : Tsetup, TtrigWidth, Tdeadtime
 * ADC : FID_samples
 * send data to PC : T_transferF 
 * user assigned : Ttr, TreadoutF, Tpw0, Tdpw, npw
 * computed : Tres 
*/
#include <arduino.h>
#include "_PARAM.h"
#include "_SEND2PC.h"
#include "_DUE_ADC.h"
#include "_PINS.h"
#include "_TIMER.h"
#include "seq_calib.h"

double Tpw0 = 2.0;  // us
double Tdpw = 0.2;  // us
int npw = 20;       //
int iq = 0;

int16_t compileCalib()
{
// 1. determine time parameters **********
  T_transferF = get_transfer_time(1, FID_samples);
  Tres = Ttr - Tsetup- TtrigWidth - Tpw0 - Tdeadtime - TreadoutF - T_transferF;  // us

// 2. Setup ADC  **********
  int er = DUE_ADC_setup(TreadoutF, 1, FID_samples); 
  if ( er > 0)     // error 
    return(er);     
   
// 3. construct events array **********
  int i,j;
  for (i = 0; i < npw; i++)
  {
    j  = i * 7;   // 7 events per cycles
//  setEvent(ID, PortC_State(Trigger, I, Q, Read, transfer) , rc, nextID);
    setEvent(j+0, PortC_State(0,0,0,0,0), Tsetup, j+1); 
    setEvent(j+1, PortC_State(1,0,0,0,0), TtrigWidth, j+2);
    if (iq == 0) 
      setEvent(j+2, PortC_State(0,1,0,0,0), Tpw0 + (double)i*Tdpw, j+3);
    else if (iq == 1)
      setEvent(j+2, PortC_State(0,0,1,0,0), Tpw0 + (double)i*Tdpw, j+3);
    setEvent(j+3, PortC_State(0,0,0,0,0), Tdeadtime, j+4);
    setEvent(j+4, PortC_State(0,0,0,1,0), TreadoutF, j+5);
    setEvent(j+5, PortC_State(0,0,0,0,1), T_transferF, j+6);
    setEvent(j+6, PortC_State(0,0,0,0,0), Tres - (double)i*Tdpw, j+7);
  }
  setEvent(j+7, PortC_State(0,0,0,0,0) , Tsetup, runMode);
  return(0);      // success
}
