/*
 * this library use digital pins of C_PORT to output I/Q on/OFF signal
 */
#include <arduino.h>
#include "_PINS.h"
#include "_PARAM.h"
#include "_DUE_ADC.h"
#include "_SEND2PC.h"

/******************** pins variables ********************/
uint8_t pin_trig;
uint8_t pin_I;
uint8_t pin_Q;
uint8_t pin_Readout;
uint8_t pin_tx;

/******************** C_PORT bit of the pins ********************/
uint8_t C_trig_bit;
uint8_t C_I_bit;
uint8_t C_Q_bit;
uint8_t C_Readout_bit;
uint8_t C_tx_bit;

uint32_t C_PortBitOfPin(uint8_t pin) // return C_PORT bit of the pin
{
  switch (pin)
  {
    case 33: return(1); break;
    case 34: return(2); break;
    case 35: return(3); break;
    case 36: return(4); break;
    case 37: return(5); break;
    case 38: return(6); break;
    case 39: return(7); break;
    case 40: return(8); break;
    case 41: return(9); break;
    case 51: return(12); break;
    case 50: return(13); break;
    case 49: return(14); break;
    case 48: return(15); break;
    case 47: return(16); break;
    case 46: return(17); break;
    case 45: return(18); break;
    case 44: return(19); break;
    case 9: return(21); break;
    case 8: return(22); break;
    case 7: return(23); break;
    case 6: return(24); break;
    case 5: return(25); break;
    case 4: return(26); break;
    case 3: return(28); break;
    case 10: return(29); break;
    default: return(0); break;
  }
}

void dataReadout(){
  DUE_ADC_read();
}

void dataSendToPC(){
  sendData2PC(DUE_ADC_I_buf, DUE_ADC_Q_buf);
  //sendData2PC(DUE_ADC_Q_buf);
}

void pulsePinsInit(uint8_t pTrig, uint8_t pI, uint8_t pQ, uint8_t pRd, uint8_t ptx)
{
  pin_trig = pTrig;
  pin_I = pI;
  pin_Q = pQ;
  pin_Readout = pRd;
  pin_tx = ptx;
  pinMode(pin_trig,OUTPUT);
  pinMode(pin_I,OUTPUT);
  pinMode(pin_Q,OUTPUT);
  pinMode(pin_Readout,OUTPUT);
  pinMode(pin_tx,OUTPUT);
  C_trig_bit = C_PortBitOfPin(pin_trig);
  C_I_bit = C_PortBitOfPin(pin_I);
  C_Q_bit = C_PortBitOfPin(pin_Q);
  C_Readout_bit = C_PortBitOfPin(pin_Readout);
  C_tx_bit = C_PortBitOfPin(pin_tx);
  attachInterrupt(digitalPinToInterrupt(pin_Readout), dataReadout, FALLING);    // Active LOW
  attachInterrupt(digitalPinToInterrupt(pin_tx), dataSendToPC, RISING);         // Active High
}


uint32_t PortC_State(uint32_t Trig_st, uint32_t I_st, uint32_t Q_st, uint32_t Re_st, uint32_t tx_st)
{
  return((Trig_st << C_trig_bit) +(I_st << C_I_bit) + (Q_st << C_Q_bit) + (Re_st << C_Readout_bit) + (tx_st << C_tx_bit));
}
