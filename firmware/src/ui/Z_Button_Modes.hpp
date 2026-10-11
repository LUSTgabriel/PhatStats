
/*
  Portrait-only mode selector for the current LOLIN build.

  The physical button/counter is retained for compatibility with the upstream
  sketch structure, but all four positions intentionally resolve to the same
  portrait renderer. The Windows host does not control display orientation.
*/

void button_Modes() {

  int enc_buttonVal = digitalRead(mode_Button);
  if (enc_buttonVal == LOW)
  {
    delay(debounceButton); // Debounce Button
    display_Button_counter++;

    /* Keep the upstream four-position local counter. */
    if (display_Button_counter >= 4)
    {
      display_Button_counter = 0;
    }
  }

  switch (display_Button_counter) {
    case 0:
    case 1:
    case 2:
    case 3:
    default:
      Display_Port_Batt();
      break;
  }
}
