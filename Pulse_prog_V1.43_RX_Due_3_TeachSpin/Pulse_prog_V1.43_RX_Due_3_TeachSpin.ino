#include <math.h>
#include "_PARAM.h"
#include "_AD9959.h"
#include "_DUE_ADC.h"
#include "_SEND2PC.h"
#include "_PINS.h"
#include "_TIMER.h"
#include "seq_calib.h"
#include "seq_cpmg.h"
#include "seq_IR.h"
#include "seq_tuning.h"

typedef int16_t (*SeqCompiler) ();
SeqCompiler activeSeqCompile;

String NMRcmd;
int16_t er;

void setup() {
  /******************** setup serial port ********************/
  SerialUSB.begin(115200);        // Initialize USB Native port
  while (!SerialUSB);             // Wait until connection is established
  /******************** Initialize AD9959  ********************/
  AD9959_Init();
  AD9959_SetFreq(0xF0, refFrequency);
  AD9959_SetAmp(0x10, ddsAmp[0]);
  AD9959_SetAmp(0x20, ddsAmp[1]);
  AD9959_SetAmp(0x40, ddsAmp[2]);
  AD9959_SetAmp(0x80, ddsAmp[3]);
  AD9959_SetPhase(0x10, ddsPh[0]);
  AD9959_SetPhase(0x20, ddsPh[1]);
  AD9959_SetPhase(0x40, ddsPh[2]);
  AD9959_SetPhase(0x80, ddsPh[3]);
  /******************** assign signal pins (IMPORTANT! use only pins of port C) **********/
  pulsePinsInit(45, 38, 33, 48, 51);  // pulsePinsInit(Trigger, I, Q, readout or blanking, transfer);
  /******************** initialize timer counter TC2 channel 1 :: TC7 **********/
  TimerInit(TC2, 1, TC7_IRQn, TC_CMR_TCCLKS_TIMER_CLOCK2);
  /******************** Initialize ADC ********************/
  DUE_ADC_Init();
}

void loop()
{
  if (SerialUSB.available() > 0 )
  {
    if (seqRunning) stopSeq();
    NMRcmd = SerialUSB.readStringUntil(':');
    /******************************** operations ********************************/
    if (NMRcmd == "stop")
    {
      stopSeq();
      seqRunning = false;
      sendText2PC("Sequence is stoped !!");
    }
    else if (NMRcmd == "run")
    {
      seqRunning = true;
      nAcq = 0;
      sendText2PC("Sequence is running !!");
      startSeq();
    }
    else  // change parameters
    {
      /**************************** technical parameters ****************************/
      if (NMRcmd == "dt")              // us
      {
        Tdeadtime = SerialUSB.parseFloat();
        sendText2PC("Dead time = " + (String)Tdeadtime + " us");
      }
      /**************************** general NMR parameters ****************************/
      else if (NMRcmd == "mode")
      {
        runMode = SerialUSB.parseInt();
        if (runMode == 0)
          sendText2PC("Run mode  = Interactive setup mode.(0)");
        else if (runMode == -1)
          sendText2PC("Run mode = Data collection mode.(-1)");
      }
      else if (NMRcmd == "navg")
      {
        nAvg = SerialUSB.parseInt();
        sendText2PC("number of signal averaging  = " + (String)nAvg);
      }
      else if (NMRcmd == "freq")             // MHz;  
      {
        refFrequency = SerialUSB.parseFloat() * 1000000;  // convert MHz to Hz
        AD9959_SetFreq(0xF0, refFrequency);     // set ref frequncy
        sendText2PC("Reference frequency = " + (String)refFrequency + " Hz");
      }
      else if (NMRcmd == "p90")             // us
      {
        Tp90 = SerialUSB.parseFloat();
        sendText2PC("90 degree pulse = " + (String)Tp90 + " us");
      }
      else if (NMRcmd == "p180")            // us
      {
        Tp180 = SerialUSB.parseFloat();
        sendText2PC("180 degree pulse = " + (String)Tp180 + " us");
      }
      else if (NMRcmd == "tr")              // us
      {
        Ttr = SerialUSB.parseFloat() * 1000;  // input (ms) -> need conversion       
        sendText2PC("Repetition time = " + (String)Ttr + " us");
      }
      else if (NMRcmd == "trdf")             // us
      {
        TreadoutF = SerialUSB.parseFloat() * 1000;  // input (ms) -> need conversion
        sendText2PC("FID readout time = " + (String)TreadoutF + " us");
      }
      else if (NMRcmd == "trde")             // us
      {
        TreadoutE = SerialUSB.parseFloat() * 1000;  // input (ms) -> need conversion
        sendText2PC("Echo readout time = " + (String)TreadoutE + " us");
      }
      else if (NMRcmd == "fsam")            // us
      {
        FID_samples = SerialUSB.parseInt();
        sendText2PC("Samples one FID = " + (String)FID_samples + " samples/FID");
      }
      else if (NMRcmd == "esam")            // us
      {
        Echo_samples = SerialUSB.parseInt();
        sendText2PC("Samples per Echo = " + (String)Echo_samples + " samples/Echo");
      }
      /******************************* DDS parameters *********************************/
      else if (NMRcmd == "f0")            // DDS amplitude ch0
      {
        ddsFreq[0] = SerialUSB.parseFloat() * 1000000;  // convert MHz to Hz
        AD9959_SetFreq(0x10, ddsFreq[0]);
        sendText2PC("CH0 frequency = " + (String)ddsFreq[0] + " Hz");
      }
      else if (NMRcmd == "f1")            // DDS amplitude ch0
      {
        ddsFreq[1] = SerialUSB.parseFloat() * 1000000;  // convert MHz to Hz
        AD9959_SetFreq(0x20, ddsFreq[1]);
        sendText2PC("CH1 frequency = " + (String)ddsFreq[1] + " Hz");
      }
      else if (NMRcmd == "f2")            // DDS amplitude ch0
      {
        ddsFreq[2] = SerialUSB.parseFloat() * 1000000;  // convert MHz to Hz
        AD9959_SetFreq(0x40, ddsFreq[2]);
        sendText2PC("CH2 frequency = " + (String)ddsFreq[2] + " Hz");
      }
      else if (NMRcmd == "f3")            // DDS amplitude ch0
      {
        ddsFreq[3] = SerialUSB.parseFloat() * 1000000;  // convert MHz to Hz
        AD9959_SetFreq(0x80, ddsFreq[3]);
        sendText2PC("CH3 frequency = " + (String)ddsFreq[3] + " Hz");
      }
      else if (NMRcmd == "amp0")            // DDS amplitude ch0
      {
        ddsAmp[0] = SerialUSB.parseInt();
        AD9959_SetAmp(0x10, ddsAmp[0]);
        sendText2PC("CH0 amplitude = " + (String)ddsAmp[0] + " ");
      }
      else if (NMRcmd == "amp1")            // DDS amplitude ch1
      {
        ddsAmp[1] = SerialUSB.parseInt();
        AD9959_SetAmp(0x20, ddsAmp[1]);
        sendText2PC("CH1 amplitude = " + (String)ddsAmp[1] + " ");
      }
      else if (NMRcmd == "amp2")            // DDS amplitude ch2
      {
        ddsAmp[2] = SerialUSB.parseInt();
        AD9959_SetAmp(0x40, ddsAmp[2]);
        sendText2PC("CH2 amplitude = " + (String)ddsAmp[2] + " ");
      }
      else if (NMRcmd == "amp3")            // DDS amplitude ch3
      {
        ddsAmp[3] = SerialUSB.parseInt();
        AD9959_SetAmp(0x80, ddsAmp[3]);
        sendText2PC("CH3 amplitude = " + (String)ddsAmp[3] + " ");
      }
      else if (NMRcmd == "ph0")             // DDS phase ch0
      {
        ddsPh[0] = SerialUSB.parseFloat();
        AD9959_SetPhase(0x10, ddsPh[0]);
        sendText2PC("CH0 phase = " + (String)ddsPh[0] + " degree");
      }
      else if (NMRcmd == "ph1")             // DDS phase ch1
      {
        ddsPh[1] = SerialUSB.parseFloat();
        AD9959_SetPhase(0x20, ddsPh[1]);
        sendText2PC("CH1 phase = " + (String)ddsPh[1] + " degree");
      }
      else if (NMRcmd == "ph2")             // DDS phase ch2
      {
        ddsPh[2] = SerialUSB.parseFloat();
        AD9959_SetPhase(0x40, fmod(ddsPh[2]+180,360));    // this output is reverse -> it need 180 deg compensation
        sendText2PC("CH2 phase = " + (String)ddsPh[2] + " degree");
      }
      else if (NMRcmd == "ph3")             // DDS phase ch3 
      {
        ddsPh[3] = SerialUSB.parseFloat();
        AD9959_SetPhase(0x80, fmod(ddsPh[3]+180,360));    // this output is reverse -> it need 180 deg compensation
        sendText2PC("CH3 phase = " + (String)ddsPh[3] + " degree");
      }
  
      /****************************** tuning parameters *******************************/
      else if (NMRcmd == "tuning")    // compile pulse sequence
      {
        activeSeqCompile = compileTuning;
        er = activeSeqCompile();
        //reportADCStatus();
        sendText2PC("Repetition time = " + (String)Ttr + " us");
        sendText2PC("FID readout time = " + (String)TreadoutF + " us");
        sendText2PC(">>> Important!! Residue time = " + (String)Tres + " us (must > 0)");
        if (er == 0 && Tres > 0)
          sendText2PC("Compile success [active sequence -> tuning]");
        else
          SerialUSB.println("Compilation ERROR!!!");
      }
  
      /******************************* calib parameters ******************************/
      else if (NMRcmd == "pw0") 
      {
        Tpw0 = SerialUSB.parseFloat();          // input (us)
        sendText2PC("Starting pulse width = " + (String)Tpw0 + " us");
      }
      else if (NMRcmd == "dpw")   
      {
        Tdpw = SerialUSB.parseFloat();          // input (us)
        sendText2PC("Stepping pulse width = " + (String)Tdpw + " us");
      }
      else if (NMRcmd == "npw")    
      {
        npw = SerialUSB.parseInt();
        sendText2PC("Number of stepping = " + (String)npw + "");
      }
      else if (NMRcmd == "iq")    
      {
        iq = SerialUSB.parseInt();
        if (iq>1) iq = 0;
        if (iq == 0) sendText2PC("calibration I pulse.");
        else if (iq == 1) sendText2PC("calibration Q pulse.");
      }      
      else if (NMRcmd == "calib")    // compile pulse sequence     
      {
        activeSeqCompile = compileCalib;
        er = activeSeqCompile();
        //reportADCStatus();
        sendText2PC("Repetition time = " + (String)Ttr + " us");
        sendText2PC("Starting pulse width = " + (String)Tpw0 + " us");
        sendText2PC("Stepping pulse width = " + (String)Tdpw + " us");
        sendText2PC("Number of steps = " + (String)npw + "");
        sendText2PC("FID readout time = " + (String)TreadoutF + " us");
        sendText2PC(">>> Important!! Residue time = " + (String)Tres + " us (must > 0)");
        if (er == 0 && Tres > 0)
          sendText2PC("Compile success [active sequence -> calibration]");
        else
          SerialUSB.println("Compilation ERROR!!!");
      }
  
      /******************************* CPMG parameters *******************************/
      else if (NMRcmd == "te")
      {
        Tte = SerialUSB.parseFloat() * 1000;  // input (ms) -> need conversion
        sendText2PC("Echo time = " + (String)Tte + " us");
      }
      else if (NMRcmd == "ne")
      {
        ne = SerialUSB.parseInt();
        sendText2PC("Number of echoes = " + (String)ne + "");
      }
      else if (NMRcmd == "cpmg")    // compile pulse sequence
      {
        activeSeqCompile = compileCPMG;
        er = activeSeqCompile();
        //reportADCStatus();
        sendText2PC("Repetition time = " + (String)Ttr + " us");
        //sendText2PC("Minimum Repetition time = " + (String)((double)ne*DUE_ADC_samples*T_PC_read_factor) + " us!!");
        sendText2PC("Echo time = " + (String)Tte + " us");
        sendText2PC("Number of echoes = " + (String)ne + "");
        sendText2PC("Echo readout time = " + (String)TreadoutE + " us");
        sendText2PC(">>> Important!! Residue time = " + (String)Tres + " us (must > 0)");
        if (er == 0 && Tres > 0)
          sendText2PC("Compile success [active sequence -> CPMG]");
        else
          SerialUSB.println("Compilation ERROR!!!");
      }
  
      /******************************** IR parameters ********************************/
      else if (NMRcmd == "irtau")
      {
        IRtau = SerialUSB.parseFloat() * 1000;  // input (ms) -> need conversion
        sendText2PC("Tau = " + (String)IRtau + " us");
      }
      else if (NMRcmd == "nir")
      {
        nIR = SerialUSB.parseInt();
        sendText2PC("IR steps = " + (String)nIR + "");
      }
      else if (NMRcmd == "ir")    // compile pulse sequence
      {
        activeSeqCompile = compileIR;
        er = activeSeqCompile();
        //reportADCStatus();
        sendText2PC("Repetition time = " + (String)Ttr + " us");
        sendText2PC("Tau = " + (String)IRtau + " us");
        sendText2PC("IR steps = " + (String)nIR + "");
        sendText2PC("FID readout time = " + (String)TreadoutF + " us");
        sendText2PC(">>> Important!! Residue time = " + (String)Tres + " us (must > 0)");
        if (er == 0 && Tres > 0)
          sendText2PC("Compile success [active sequence -> Inversion Recovery]");
        else
          SerialUSB.println("Compilation ERROR!!!");
      } // end of commands excuting
      
      /******************************** interactive mode ********************************/
      if ((runMode == 0) && seqRunning ) 
      {
          er = activeSeqCompile();  // need re compile and run after parameter changed
        if (er == 0)
          startSeq();
      }
    }
  }
}
