using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    public Button ACPowerBtn;
    public Button TVBtn;
    public Button BlindBtn;
    public Button MoodBtn;
    public Button SinkBtn;
    public Text TempText;
    public Text HumidityText;
    public Text setTempText;
    public Text setFanText;

    public int celsiusValue;
    public int humidityValue;
    public int setTempValue;
    public int powerLevel;

    public Color disableColor;
    public Color activityColor;
    
    // Start is called before the first frame update
    void Start()
    {
        ColorUtility.TryParseHtmlString("#D9D9D9", out disableColor);
        ColorUtility.TryParseHtmlString("#FF8D8D", out activityColor);
    }

    // Update is called once per frame
    void Update()
    {
        try{
            celsiusValue = MainController.instance.getCelcius();
            humidityValue = MainController.instance.getHumidity();
            if(celsiusValue != 0)       TempText.text = celsiusValue.ToString();
            if(humidityValue != 0)      HumidityText.text = humidityValue.ToString();

            setTempValue = MainController.instance.getSetTemp();
            if(MainController.instance.isACOn()){
                setTempText.text = setTempValue.ToString();
            } else{
                setTempText.text = "--";
            }

            powerLevel = MainController.instance.getPowerLevel();
            if(!MainController.instance.isACOn()){
                setFanText.text = "";
            }else if(powerLevel == 1){
                setFanText.text = "✇";
            } else if(powerLevel == 2){
                setFanText.text = "✇✇";
            }


            /* Change the Color from Sensor */
            changeBtnColor(ACPowerBtn, MainController.instance.isACOn());
            changeBtnColor(TVBtn, MainController.instance.isTVOn());
            changeBtnColor(BlindBtn, MainController.instance.isBlindOn());
            changeBtnColor(MoodBtn, MainController.instance.isMoodOn());
            changeBtnColor(SinkBtn, MainController.instance.isSinkOn());
        } catch(System.Exception){

        }
        
    }

    public void changeBtnColor(Button button, bool isActive){
        ColorBlock colorBlock = button.colors;
        if(isActive){
            colorBlock.normalColor = activityColor;
            colorBlock.selectedColor = activityColor;
            colorBlock.pressedColor = activityColor;
            colorBlock.highlightedColor = activityColor;
        } else{
            colorBlock.normalColor = disableColor;
            colorBlock.selectedColor = disableColor;
            colorBlock.pressedColor = disableColor;
            colorBlock.highlightedColor = disableColor;
        }
        button.colors = colorBlock;
    }

    public void sinkAction(){
        MainController.instance.sinkAction();
    }
    public void acPowerAction(){
        MainController.instance.acPowerAction();
    }
    public void tvAction(){
        MainController.instance.tvAction();
    }
    public void lightAction(){
        MainController.instance.lightAction();
    }
    public void blindAction(){
        MainController.instance.blindAction();
    }
    public void acFanDownAction(){
        MainController.instance.acFanDownAction();
    }
    public void acFanUpAction(){
        MainController.instance.acFanUpAction();
    }
    public void acTempDownAction(){
        MainController.instance.acTempDownAction();
    }
    public void acTempUpAction(){
        MainController.instance.acTempUpAction();
    }
}
