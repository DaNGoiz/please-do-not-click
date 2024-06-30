using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/* 
 * Copyright (c) DaNGo_iz. All rights reserved.
 * 更多工具和游戏请访问网站：www.dangoiz.com
 * 默认素材：COC模组《请勿点击》的Replay Log
 * 导出删掉注释，win的图像会出问题，建议双版本
*/
/// </summary>

public class DialogSystem : MonoBehaviour
{
    public string[] namesOfNPC;
    //角色信息
    Dictionary<string, int> namesToNum = new Dictionary<string, int>();
    Dictionary<int, string> numToNames = new Dictionary<int, string>();
    Dictionary<string, Sprite> strToFaces = new Dictionary<string, Sprite>();

    public float textSpeed;
    public float autoTextWaitingSpeed;
    //文本信息
    Dictionary<string, int> strToIntTexts = new Dictionary<string, int>();
    Dictionary<int, string> intToStrTexts = new Dictionary<int, string>();
    
    float tempTextSpeed; //用于记录用户设置的文字速度
    int index; //显示阅读的行数

    //场景信息
    Dictionary<string, Sprite> strToScenes = new Dictionary<string, Sprite>();
    Dictionary<string, Sprite> strToNewChapterFitstScene = new Dictionary<string, Sprite>();
    Dictionary<int, string> intToNCFSstr = new Dictionary<int, string>();
    Dictionary<string, Sprite> strToForegroundScenes = new Dictionary<string, Sprite>();

    //public Sprite[] newChapterSprite; //新章节播放的第一个图像

    [Header("Music")]
    public AudioClip[] bgmClips;
    public AudioClip[] bgsClips;
    public AudioClip[] typingSoundClips;
    [Range(0, 1)]
    public float typingVolume;
    [Space(15)]
    public AudioSource diceSound;

    //音频信息
    AudioSource bgmSources;
    bool changeBGM;
    int bgmIndex;

    AudioSource bgsSources;
    bool changeBGS;
    int bgsIndex;

    AudioSource typingSound;

    [Header("UI Components")]
    //screen components
    [Space(50)]
    public GameObject chapterButtonPrefab;
    public Text textLabel;
    public Image littleArrow; //中下方的小箭头
    public Image shining; //闪烁效果的贴图
    public Image background;
    public Canvas foreground;
    public float spriteSpeed;
    public Button menuButton;
    public Button autoButton;
    public Button autoButtonClosed;
    List<string> textList = new List<string>(); //定义一个链表以储存文本

    //menu
    public Canvas menu;
    int chapterNumber;
    int maxChapter;

    //camera
    float shakeTime = 1.0f;//震动时间
    private float currentTime = 0.0f;
    private List<Vector3> gameobjpons = new List<Vector3>();
    public Camera cameraToShake;//要求震动的相机
    bool shake;

    //slot
    public Image[] faceImage;
    public Image[] nameImage;
    public Text[] nameLabel;
    bool[] slotEmpty = new bool[4];

    //bools
    bool textFinished; //为了确保每一行播放结束后才开始读取下一行（从而避免乱码），设置布尔值判断文本是否读完
    bool cancelTyping; //为了让玩家按按键可以再次跳过对话，写了取消打字模式
    bool bigger; //判断头像是否放大
    bool betterInterface; //为了知道当前画面里有几个显示的槽位并改变排版
    bool isChangingScene; //让切换场景的时候稍等
    bool menuIsOpen; //menu是否打开
    bool foregroundIsOpen; //前景显示
    bool auto; //自动播放
    bool nextLine; //允许播放下一行
    bool getTypingSound; //已经获得打字声

    //初始化与进程
    void Awake() //初始设置1
    {
        TextCreate();
        menu.gameObject.SetActive(false);

        GetTextFromFile(intToStrTexts[0]); //在开始的时候接收到切分好每一行的链表
        tempTextSpeed = textSpeed; //在开始就接收初始的文本播放速度

        //设置slot布尔为false，设置slot不可见
        for (int i = 0; i < 4; i++)
        {
            slotEmpty[i] = true;
            SlotInvisible(faceImage[i], nameImage[i], nameLabel[i]);
        }

        //创建角色名映射
        for (int i = 0; i < namesOfNPC.Length; i++)
        {
            namesToNum.Add(namesOfNPC[i], i);
            numToNames.Add(i, namesOfNPC[i]);
        }

        //背景映射
        FileInfo[] backgroundsFileName = AllFileNames(Application.streamingAssetsPath + "/Background");
        for (int i = 0; i < backgroundsFileName.Length; i++)
        {
            strToScenes.Add(backgroundsFileName[i].Name, GetSpriteFromFilePath(Application.streamingAssetsPath + "/Background/" + backgroundsFileName[i].Name));
        }

        //新章节背景映射+加载第一个章节背景图
        FileInfo[] backgroundsNewFileName = AllFileNames(Application.streamingAssetsPath + "/BackgroundInNewChapter");
        for (int i = 0; i < backgroundsNewFileName.Length; i++)
        {
            strToNewChapterFitstScene.Add(backgroundsNewFileName[i].Name, GetSpriteFromFilePath(Application.streamingAssetsPath + "/BackgroundInNewChapter/" + backgroundsNewFileName[i].Name));
            intToNCFSstr.Add(i, backgroundsNewFileName[i].Name);
        }
        background.sprite = strToNewChapterFitstScene["sc00.png"];

        //前景映射
        FileInfo[] foregroundsFileName = AllFileNames(Application.streamingAssetsPath + "/Foreground");
        for (int i = 0; i < foregroundsFileName.Length; i++)
        {
            strToForegroundScenes.Add(foregroundsFileName[i].Name, GetSpriteFromFilePath(Application.streamingAssetsPath + "/Foreground/" + foregroundsFileName[i].Name));
        }

        //头像映射+读取本地头像
        FileInfo[] characterFacesFileName = AllFileNames(Application.streamingAssetsPath + "/Characters");
        for (int i = 0; i < characterFacesFileName.Length; i++)
        {
            strToFaces.Add(characterFacesFileName[i].Name, GetSpriteFromFilePath(Application.streamingAssetsPath + "/Characters/" + characterFacesFileName[i].Name));
        }
    }

    private void OnEnable() //初始设置2
    {
        bgmSources = this.gameObject.AddComponent<AudioSource>();
        bgmSources.loop = true;
        bgmSources.volume = 0.6f;
        bgmSources.pitch = 1; //pitch

        bgsSources = this.gameObject.AddComponent<AudioSource>();
        bgsSources.loop = false;
        bgsSources.volume = 1f;

        typingSound = this.gameObject.AddComponent<AudioSource>();
        typingSound.loop = false;
        typingSound.playOnAwake = false;
        typingSound.clip = typingSoundClips[typingSoundClips.Length - 1];

        //使得在panel启用时就输出第一句话。因为会在start之前调用，所以上面原本是start，现在换为awake
        textFinished = true;
        StartCoroutine(SetTextUI());
    }

    void Update() //侦测按键输入、控制文字播放携程等
    {
        
        if (changeBGM)
        {
            bgmSources.clip = bgmClips[bgmIndex];
            bgmSources.volume = 0.6f;
            bgmSources.Play();
            changeBGM = false;
        }

        if (changeBGS)
        {
            bgsSources.clip = bgsClips[bgsIndex];
            bgsSources.Play();
            changeBGS = false;
        }

        if (!auto)
        {
            if (Input.GetKeyDown(KeyCode.Space) && index == textList.Count && !menuIsOpen)
            {
                //如果换章节出了问题首先怀疑这里
                /*
                for (int i = 0; i < intToStrTexts.Count; i++)
                {
                    if (chapterNumber == int.Parse(intToStrTexts[i].Substring(26, 2)))
                    {
                        if (chapterNumber < maxChapter)
                        {
                            chapterNumber += 1;
                            for (int j = 0; j < intToStrTexts.Count; j++)
                            {
                                if(int.Parse(intToStrTexts[j].Substring(26, 2)) == chapterNumber && intToStrTexts[j].EndsWith(".txt"))
                                {
                                    index = 0;
                                    GetTextFromFile(intToStrTexts[j]);
                                    StartCoroutine(ChangeChapter(chapterNumber));
                                    i = intToStrTexts.Count;
                                }
                            }

                        }
                        else
                        {
                            textLabel.text = "——全文完——";
                        }
                    }
                }
                */
                textLabel.text = "——本章完——";
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) && !menuIsOpen)
            {
                if (textFinished && !cancelTyping)
                {
                    StartCoroutine(SetTextUI());

                }
                else if (!textFinished)
                {
                    cancelTyping = !cancelTyping; //小技巧：每按下一次按键，这里面的值对掉
                }
            }
        }
        else
        {
            if (nextLine && index == textList.Count && !menuIsOpen)
            {
                textLabel.text = "——本章完——";
                return;
            }

            if (nextLine && !menuIsOpen)
            {
                if (textFinished && !cancelTyping)
                {
                    StartCoroutine(SetTextUI());
                }
                else if (!textFinished)
                {
                    cancelTyping = !cancelTyping; //小技巧：每按下一次按键，这里面的值对掉
                }
            }
        }
        
        if (shake) //camera
        {
            currentTime = shakeTime;
            shake = false;
        }
    }

    void LateUpdate() { UpdateShake(); }

    //读取本地文件为素材
    void TextCreate() //文本映射，菜单布置章节按钮
    {
        FileInfo[] textFileName = AllFileNames(Application.streamingAssetsPath + "/TextFile");
        int buttonPosition = 3;
        int childIndex = 0;
        for (int i = 0; i < textFileName.Length; i++)
        {
            strToIntTexts.Add(Application.streamingAssetsPath + "/TextFile/" + textFileName[i].Name, i);
            intToStrTexts.Add(i, Application.streamingAssetsPath + "/TextFile/" + textFileName[i].Name);
            if (textFileName[i].Name.EndsWith(".txt"))
            {
                GameObject menu0 = GameObject.Find("Menu");
                GameObject button0 = Instantiate(chapterButtonPrefab);
                button0.transform.SetParent(menu0.transform);
                button0.transform.position = new Vector3(0, buttonPosition, 0);
                buttonPosition -= 1;
                //+1是给小幽灵留的
                Text currencyText = GameObject.Find("Menu").transform.GetChild(childIndex + 1).gameObject.GetComponent<Text>();
                maxChapter = childIndex;
                currencyText.text = textFileName[i].Name.Substring(2, textFileName[i].Name.Length - 6);
                button0.GetComponent<Button>().onClick.AddListener(delegate ()
                {
                    for (int j = 0; j < textFileName.Length; j++)
                    {
                        char[] buttonChar = intToStrTexts[j].ToCharArray();
                        int buttonCharIndex = 0;
                        for (int k = 0; k < buttonChar.Length; k++)
                        {
                            if(buttonChar[k] == '/')
                            {
                                buttonCharIndex = k;
                            }
                        }
                        string buttonStr = "";
                        string fullButtonStr = "";
                        for (int k = buttonCharIndex + 3; k < buttonChar.Length - 4; k++)
                        {
                            buttonStr += buttonChar[k];
                        }
                        for (int k = buttonCharIndex + 1; k < buttonChar.Length - 4; k++)
                        {
                            fullButtonStr += buttonChar[k];
                        }
                        if (button0.GetComponent<Text>().text == buttonStr)
                        {
                            auto = false;
                            autoButton.gameObject.SetActive(true);
                            autoButtonClosed.gameObject.SetActive(false);
                            ChooseChaper(fullButtonStr);
                            chapterNumber = int.Parse(intToStrTexts[j].Substring(28, intToStrTexts[j].Length - 32)); //？
                        }
                    }
                });
                childIndex++;
            }
        }
    }

    FileInfo[] AllFileNames(string path) //把本地文件夹中的所有文件加载至dict
    {
        if (Directory.Exists(path))
        {
            DirectoryInfo direction = new DirectoryInfo(path);
            //FileInfo[] files = direction.GetFiles(Application.streamingAssetsPath + "*", SearchOption.AllDirectories);
            FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Name.EndsWith(".meta"))
                {
                    continue;
                }
            }
            return files;
        }
        return null;
    }

    Sprite GetSpriteFromFilePath(string path) //从当前文件路径获取并返回sprite
    {
        byte[] ImgByte = getImageByte(path);
        Texture2D texture2D = new Texture2D(1048, 1632); //!!
        texture2D.LoadImage(ImgByte);
        Sprite sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
        return sprite;
    }

    void ChangeImage(string name, Image image) //切换图像的sprite为本地文件
    {
        string pathStr = Application.streamingAssetsPath + "/" + name + ".png";
        TextureToSprite(getImageByte(pathStr), image);
    }

    private void TextureToSprite(byte[] ImgByte, Image image) //添加纹理到sprite
    {
        Texture2D texture2D = new Texture2D(1080, 1920); //!!
        texture2D.LoadImage(ImgByte);
        Sprite sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
        image.sprite = sprite;
    }

    private static byte[] getImageByte(string imagePath) //把路径转为byte类型
    {
        FileStream files = new FileStream(imagePath, FileMode.Open);
        byte[] imgByte = new byte[files.Length];
        files.Read(imgByte, 0, imgByte.Length);
        files.Close();
        return imgByte;
    }

    //文本控制
    void GetTextFromFile(string path) //获取文本文件
    {
        string[] line = File.ReadAllLines(path);
        textList.Clear(); //先把链表清空从而清空显示
        index = 0; //重置索引

        for (int i = 0; i < line.Length; i++)
        {
            textList.Add(line[i]);
        }
    }

    //槽控制
    void BetterInterface() //调整界面排版
    {
        int num = NumberOfFullSlots();
        int[] index = IndexOfFullSlots();

        if(num == 1)
        {
            faceImage[index[0]].transform.position = new Vector3(0, 0, 0);
            nameImage[index[0]].transform.position = new Vector3(0, -1, 0);
        }
        else if(num == 2)
        {
            faceImage[index[0]].transform.position = new Vector3(-3, 0, 0);
            nameImage[index[0]].transform.position = new Vector3(-3, -1, 0);
            faceImage[index[1]].transform.position = new Vector3(3, 0, 0);
            nameImage[index[1]].transform.position = new Vector3(3, -1, 0);
        }
        else if(num == 3)
        {
            faceImage[index[0]].transform.position = new Vector3(-4.5f, 0, 0);
            nameImage[index[0]].transform.position = new Vector3(-4.5f, -1, 0);
            faceImage[index[1]].transform.position = new Vector3(0, 0, 0);
            nameImage[index[1]].transform.position = new Vector3(0, -1, 0);
            faceImage[index[2]].transform.position = new Vector3(4.5f, 0, 0);
            nameImage[index[2]].transform.position = new Vector3(4.5f, -1, 0);
        }
        else if(num == 4)
        {
            faceImage[index[0]].transform.position = new Vector3(-6, 0, 0);
            nameImage[index[0]].transform.position = new Vector3(-6, -1, 0);
            faceImage[index[1]].transform.position = new Vector3(-2, 0, 0);
            nameImage[index[1]].transform.position = new Vector3(-2, -1, 0);
            faceImage[index[2]].transform.position = new Vector3(2, 0, 0);
            nameImage[index[2]].transform.position = new Vector3(2, -1, 0);
            faceImage[index[3]].transform.position = new Vector3(6, 0, 0);
            nameImage[index[3]].transform.position = new Vector3(6, -1, 0);
        }
    }

    void AddCharacterToSreen(string name, Sprite face) //添加角色到屏幕
    {
        int i = 0;
        while (i < 4)
        {
            if (slotEmpty[i])
            {
                faceImage[i].sprite = face;
                nameLabel[i].text = name;
                slotEmpty[i] = false;
                i = 4;
            }
            i++; //原本放在if前，不知道会有什么影响
        }
    }

    int[] RestOfFour(int num) //返回0-3中除了当前数字外的所有数字
    {
        int[] three = new int[3];
        int j = 0;
        for (int i = 0; i < 4; i++)
        {
            if (i != num)
            {
                three[j] = i;
                j++;
            }
        }
        return three;
    }

    int NumberOfFullSlots() //获取一共有多少个已填充的槽
    {
        int num = 0;
        for (int i = 0; i < 4; i++)
        {
            if (!slotEmpty[i])
            {
                num++;
            }
        }
        return num;
    }

    int[] IndexOfFullSlots() //获取已填充的槽的index
    {
        int num = NumberOfFullSlots();
        int[] index = new int[num];
        int j = 0;
        for (int i = 0; i < 4; i++)
        {
            if (!slotEmpty[i])
            {
                index[j] = i;
                j++;
            }
        }
        return index;
    }

    //章节控制
    public void ChooseChaper(string chapter) //选章节，传进来的是完整的章节名
    {
        int temp = strToIntTexts[Application.streamingAssetsPath + "/TextFile/" + chapter + ".txt"];
        GetTextFromFile(intToStrTexts[temp]);
        CloseMenu();
        chapterNumber = int.Parse(chapter.Substring(0,2));
        StartCoroutine(ChangeChapter(chapterNumber));
    }

    IEnumerator ChangeChapter(int chapter) //切换章节动画+开始自动播放文本
    {
        shining.color = new Vector4(0, 0, 0, 0);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.25f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.5f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.75f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 1);
        for (int i = 0; i < 4; i++)
        {
            SlotInvisible(faceImage[i], nameImage[i], nameLabel[i]);
            SlotClear(faceImage[i], nameImage[i], nameLabel[i]);
            slotEmpty[i] = true;
            nameLabel[i].text = "";
        }
        StartCoroutine(SetTextUI());
        if (chapter < 10)
        {
            background.sprite = strToNewChapterFitstScene["sc0" + chapter.ToString() + ".png"];
        }
        else
        {
            background.sprite = strToNewChapterFitstScene["sc" + chapter.ToString() + ".png"];
        }
        
        yield return new WaitForSeconds(0.5f);
        shining.color = new Vector4(0, 0, 0, 0.75f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.5f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.25f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0);
    }

    public void OpenMenu() //显示章节选择菜单
    {
        menuIsOpen = true;
        menu.gameObject.SetActive(true);
        if (!Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(Show(0.75f, menu.GetComponent<Image>()));
            StartCoroutine(Hide(0, menuButton.image));//隐藏打开菜单按钮
            StartCoroutine(Hide(0, autoButton.image));
            StartCoroutine(Hide(0, autoButtonClosed.image));
            //？似乎可以节省代码？menuBackground.CrossFadeAlpha(1, 2f, true);
        }
    }

    public void CloseMenu() //关闭章节选择菜单
    {
        menuIsOpen = false;
        StartCoroutine(Hide(0, menu.GetComponent<Image>()));
        menu.gameObject.SetActive(false);
        StartCoroutine(Show(1, menuButton.image));
        StartCoroutine(Show(1, autoButton.image));
        StartCoroutine(Show(1, autoButtonClosed.image));
    }

    IEnumerator Show(float f, Image image) //显示效果
    {
        image.color = new Vector4(0, 0, 0, 0.25f);
        yield return new WaitForSeconds(0.1f);
        image.color = new Vector4(0, 0, 0, f);
    }

    IEnumerator ShowButton(float f, Image image) //显示效果（针对按钮）
    {
        image.color = new Vector4(1, 1, 1, 0.25f);
        yield return new WaitForSeconds(0.1f);
        image.color = new Vector4(1, 1, 1, f);
    }

    IEnumerator Hide(float f, Image image) //隐藏效果
    {
        image.color = new Vector4(0, 0, 0, 0.25f);
        yield return new WaitForSeconds(0.1f);
        image.color = new Vector4(0, 0, 0, f);
    }

    public void Auto(bool a)
    {
        auto = a;
        if (auto && textFinished && !cancelTyping)
        {
            StartCoroutine(SetTextUI());
        }
    }

    //场景控制
    IEnumerator ChangeScene(Sprite sprite) //切换场景
    {
        for (int j = 0; j < 4; j++)
        {
            CharacterGrey(slotEmpty[j], faceImage[j], nameImage[j], nameLabel[j]);
        }
        shining.color = new Vector4(0, 0, 0, 0);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.25f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.5f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.75f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 1);
        background.sprite = sprite; //调换贴图
        yield return new WaitForSeconds(0.5f);
        shining.color = new Vector4(0, 0, 0, 0.75f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.5f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.25f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0);
    }

    IEnumerator ShowForeground(Sprite sprite) //显示cg
    {
        for (int j = 0; j < 4; j++)
        {
            CharacterGrey(slotEmpty[j], faceImage[j], nameImage[j], nameLabel[j]);
        }
        shining.color = new Vector4(0, 0, 0, 0);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.25f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.5f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.75f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 1);
        foreground.gameObject.SetActive(true);
        foreground.GetComponent<Image>().sprite = sprite;
        yield return new WaitForSeconds(0.5f);
        shining.color = new Vector4(0, 0, 0, 0.75f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.5f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.25f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0);
        foregroundIsOpen = true;
        yield return new WaitForSeconds(2f);
    }

    IEnumerator CloseForeground() //关闭cg
    {
        shining.color = new Vector4(0, 0, 0, 0);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.25f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.5f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.75f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 1);
        foreground.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        shining.color = new Vector4(0, 0, 0, 0.75f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.5f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0.25f);
        yield return new WaitForSeconds(0.1f);
        shining.color = new Vector4(0, 0, 0, 0);
    }

    //图像、立绘、音乐效果（想把渐变效果都做到一个方法里）
    IEnumerator StopPlayingBGM()
    {
        bgmSources.volume = 0.3f;
        yield return new WaitForSeconds(0.5f);
        bgmSources.volume = 0.15f;
        yield return new WaitForSeconds(0.5f);
        bgmSources.volume = 0f;
    }

    IEnumerator StartPlayingBGM()
    {
        bgmSources.volume = 0.15f;
        yield return new WaitForSeconds(0.5f);
        bgmSources.volume = 0.3f;
        yield return new WaitForSeconds(0.5f);
        bgmSources.volume = 0.6f;
    }

    void CharacterGrey(bool empty, Image faceImage, Image nameImage, Text nameLabel) //角色立绘在非发言状态变灰
    {
        if (!empty)
        {
            faceImage.color = new Vector4(0.5f, 0.5f, 0.5f, 1);
            nameImage.color = new Vector4(0.5f, 0.5f, 0.5f, 1);
            nameLabel.color = new Vector4(0.5f, 0.5f, 0.5f, 1);
        }
    }

    void CharacterBright(bool empty, Image faceImage, Image nameImage, Text nameLabel) //角色立绘在发言状态变亮
    {
        if (!empty)
        {
            faceImage.color = new Vector4(1, 1, 1, 1);
            nameImage.color = new Vector4(1, 1, 1, 1);
            nameLabel.color = new Vector4(1, 1, 1, 1);
        }
    }

    IEnumerator Wait(Image image) //让图像出现的时候放大缩小
    {
        //yield return new WaitForSeconds(0.2f); //等待一下，因为可能图像没来得及切换
        if (bigger == false) //确保只放大一次
        {
            image.transform.localScale += new Vector3(0.003f, 0.003f, 0);
            bigger = true;
            yield return new WaitForSeconds(spriteSpeed); //等待时间
            image.transform.localScale -= new Vector3(0.003f, 0.003f, 0);
        }
        bigger = false;
    }

    IEnumerator Shine(Image image, int r, int g, int b) //闪烁效果
    {
        image.color = new Vector4(r, g, b, 0);
        yield return new WaitForSeconds(0.1f);
        image.color = new Vector4(r, g, b, 0.25f);
        yield return new WaitForSeconds(0.1f);
        image.color = new Vector4(r, g, b, 0.5f);
        yield return new WaitForSeconds(0.1f);
        image.color = new Vector4(r, g, b, 0.75f);
        yield return new WaitForSeconds(0.1f);
        image.color = new Vector4(r, g, b, 1);
        yield return new WaitForSeconds(0.3f);
        image.color = new Vector4(r, g, b, 0.75f);
        yield return new WaitForSeconds(0.1f);
        image.color = new Vector4(r, g, b, 0.5f);
        yield return new WaitForSeconds(0.1f);
        image.color = new Vector4(r, g, b, 0.25f);
        yield return new WaitForSeconds(0.1f);
        image.color = new Vector4(r, g, b, 0);
        yield return new WaitForSeconds(0.1f);
    }

    void SlotInvisible(Image faceImage,Image nameImage,Text nameLabel) //让人物槽直接不显示
    {
        faceImage.color = new Vector4(0, 0, 0, 0);
        nameImage.color = new Vector4(0, 0, 0, 0);
        nameLabel.color = new Vector4(0, 0, 0, 0);
    }

    void UpdateShake() //相机震动
    {
        if (currentTime > 0.0f)
        {
            currentTime -= Time.deltaTime;
            cameraToShake.rect = new Rect(0.04f * (-1.0f + 2.0f * Random.value) * Mathf.Pow(currentTime, 2), 0.04f * (-1.0f + 2.0f * Random.value) * Mathf.Pow(currentTime, 2), 1.0f, 1.0f);
        }
        else
        {
            currentTime = 0.0f;
        }
    }

    void SlotClear(Image faceImage, Image nameImage, Text nameLabel)
    {
        faceImage = null;
        nameImage = null;
        nameLabel = null;
    }

    void OnOff() //上场 & 退场，和下面的操控部分一起用
    {
        if (textList[index].Substring(textList[index].Length - 2, 2) == "上场")
        {
            for (int k = 0; k < namesOfNPC.Length; k++)
            {
                if (textList[index] == numToNames[k] + textList[index].Substring(textList[index].Length - 4, 4))
                {
                    AddCharacterToSreen(numToNames[k], strToFaces[numToNames[k] + textList[index].Substring(textList[index].Length - 4, 2) + ".png"]);
                    index++;
                }
            }
        }
        else if (textList[index].Substring(textList[index].Length - 2, 2) == "退场")
        {
            for (int k = 0; k < namesOfNPC.Length; k++)
            {
                if (textList[index] == numToNames[k] + textList[index].Substring(textList[index].Length - 2, 2))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (nameLabel[i].text == numToNames[k])
                        {
                            nameLabel[i].text = "";
                            slotEmpty[i] = true;
                            SlotInvisible(faceImage[i], nameImage[i], nameLabel[i]);
                            index++;
                        }
                    }
                }
            }
        }
    }

    IEnumerator SetTextUI() //逐字读取和特殊操作
    {
        typingSound.volume = typingVolume; //回归打字声
        textLabel.color = new Vector4(0, 0, 0, 1); //字体颜色变回黑色
        textFinished = false; //开始打字，当前行没有读完
        textLabel.text = ""; //在携程开始时把文本框清空，这样每次才能从最开始播放
        littleArrow.color = new Vector4(1, 1, 1, 0); //小箭头
        nextLine = false; //不能跳行

        if (foregroundIsOpen)
        {
            StartCoroutine(CloseForeground());
            foregroundIsOpen = false;
            yield return new WaitForSeconds(0.5f);
        }

        //上场 & 退场
        //做8次是为了防止index++跳行，确保哪怕八个上下场指令连在一起都生效。最多也就四个人下场换四个人上吧。。。
        for (int i = 0; i < 8; i++)
        {
            OnOff();
        }

        //识别名字行并切换立绘
        if (textList[index].Substring(textList[index].Length - 3, 1) == "：")
        {
            for (int k = 0; k < namesOfNPC.Length; k++)
            {
                if (textList[index] == numToNames[k] + "：" + textList[index].Substring(textList[index].Length - 2, 2))
                {
                    //找到对应的slot
                    for (int i = 0; i < 4; i++)
                    {
                        if (nameLabel[i].text == numToNames[k])
                        {
                            //让当前槽亮，其他槽暗
                            int[] rest = RestOfFour(i);
                            for (int j = 0; j < 3; j++)
                            {
                                CharacterGrey(slotEmpty[rest[j]], faceImage[rest[j]], nameImage[rest[j]], nameLabel[rest[j]]);
                            }
                            faceImage[i].sprite = strToFaces[numToNames[k] + textList[index].Substring(textList[index].Length - 2, 2) + ".png"];
                            nameLabel[i].text = numToNames[k];
                            CharacterBright(slotEmpty[i], faceImage[i], nameImage[i], nameLabel[i]);
                            bigger = false;
                            StartCoroutine(Wait(faceImage[i]));

                            if (k < typingSoundClips.Length - 1)
                            {
                                typingSound.clip = typingSoundClips[k];
                            }
                            else
                            {
                                typingSound.clip = typingSoundClips[typingSoundClips.Length - 1];
                            }
                        }
                    }
                    index++;
                }
            }
        }
        else if (textList[index].Substring(textList[index].Length - 1, 1) == "：") //如果冒号后没有数字就播默认表情
        {
            for (int k = 0; k < namesOfNPC.Length; k++)
            {
                if (textList[index] == numToNames[k] + "：")
                {
                    //找到对应的slot
                    for (int i = 0; i < 4; i++)
                    {
                        if (nameLabel[i].text == numToNames[k])
                        {
                            

                            //让当前槽亮，其他槽暗
                            int[] rest = RestOfFour(i);
                            for (int j = 0; j < 3; j++)
                            {
                                CharacterGrey(slotEmpty[rest[j]], faceImage[rest[j]], nameImage[rest[j]], nameLabel[rest[j]]);
                            }
                            faceImage[i].sprite = strToFaces[numToNames[k] + "00.png"];
                            nameLabel[i].text = numToNames[k];
                            CharacterBright(slotEmpty[i], faceImage[i], nameImage[i], nameLabel[i]);
                            bigger = false;
                            StartCoroutine(Wait(faceImage[i]));
                        }
                    }
                    index++;
                }
            }
        }

        if (textList[index].Length == 1)
        {
            Debug.Log("目前一行只有一个字会报错，可以先在后面加句号/省略号等");
        }

        //特殊指令管理器
        int letter = 0;
        while (letter < textList[index].Length)
        {
            bool typing = true;
            //用特殊字符判断是否让文字产生特殊变化的方法，但是目前无法做到只让部分字体变大/小
            if (textList[index][letter] == '#')
            {
                typing = false;
                letter++;
                switch (textList[index][letter])
                {
                    case '>': //规则：加在最后或者连续加要按一下空格键接着
                        textLabel.fontSize += 10; //！！！问题出在这里，只能让整体字体变大/小
                        break;
                    case '<':
                        textLabel.fontSize -= 10;
                        break;
                    case 'b': //bgm, bgs
                        letter++;
                        switch (textList[index][letter])
                        {
                            case 'g':
                                letter++;
                                switch (textList[index][letter])
                                {
                                    case 'm': //bgm
                                        letter++;

                                        if(textList[index][letter] == 'x') //bgmx, turn off bgm
                                        {
                                            //letter++;
                                            StartCoroutine(StopPlayingBGM());
                                        }
                                        else if(textList[index][letter] == 'o') //bgmo, turn on bgm
                                        {
                                            //letter++;
                                            StartCoroutine(StartPlayingBGM());
                                        }
                                        else
                                        {
                                            char[] csBgm = { textList[index][letter], textList[index][letter + 1] };
                                            letter++;
                                            string strBgm = new string(csBgm);
                                            bgmIndex = int.Parse(strBgm);
                                            changeBGM = true;
                                        }
                                        break;
                                    case 's': //bgs
                                        letter++;
                                        char[] csBgs = { textList[index][letter], textList[index][letter + 1] };
                                        letter++;
                                        string strBgs = new string(csBgs);
                                        bgsIndex = int.Parse(strBgs);
                                        changeBGS = true;
                                        break;
                                }
                                break;
                        }
                        break;
                    case 'c': //font color
                        letter++;
                        switch (textList[index][letter])
                        {
                            case 'b': //blue
                                textLabel.color = new Vector4(0, 0, 1, 1);
                                break;
                            case 'g': //cg
                                letter++;
                                char[] cs = { textList[index][letter], textList[index][letter + 1] };
                                letter++;
                                string str = new string(cs);
                                str = "cg" + str + ".png";
                                StartCoroutine(ShowForeground(strToForegroundScenes[str]));
                                break;
                            case 'r': //red
                                textLabel.color = new Vector4(1, 0, 0, 1);
                                break;
                            case 'h': //black
                                textLabel.color = new Vector4(0, 0, 0, 1);
                                break;
                        }
                        break;
                    case 'd': //dice sound
                        diceSound.Play();
                        break;
                    case 'k': //kp = narrator
                        letter++;
                        switch (textList[index][letter])
                        {
                            case 'p':
                                for (int i = 0; i < 4; i++)
                                {
                                    CharacterGrey(slotEmpty[i], faceImage[i], nameImage[i], nameLabel[i]);
                                }
                                typingSound.clip = typingSoundClips[typingSoundClips.Length - 1];
                                break;
                        }
                        break;
                    case 'l': //light
                        letter++;
                        switch (textList[index][letter])
                        {
                            case 'b': //black
                                StartCoroutine(Shine(shining, 0, 0, 0)); //闪耀贴图，rgb
                                break;
                            case 'r': //red
                                StartCoroutine(Shine(shining, 1, 0, 0));
                                break;
                            case 'w': //white
                                StartCoroutine(Shine(shining, 1, 1, 1));
                                break;
                        }
                        break;
                    case 's': //speed && scene （只对当前这句话有效）
                        letter++;
                        switch (textList[index][letter])
                        {
                            case '>':
                                textSpeed *= 1 / 3;
                                break;
                            case '<':
                                textSpeed *= 6;
                                break;
                            case 'c': //scene
                                letter++;
                                char[] cs = { textList[index][letter], textList[index][letter + 1] };
                                letter++;
                                string str = new string(cs);
                                str = "sc" + str + ".png";
                                StartCoroutine(ChangeScene(strToScenes[str]));
                                isChangingScene = true;
                                break;
                        }
                        break;
                    case 'y': //camera shake
                        shake = true;
                        break;
                }
                if(letter < textList[index].Length - 1) //确保不超出index
                {
                    letter++;
                }
            }
            
            if(typing)
            {
                typingSound.Play();
            }

            textLabel.text += textList[index][letter]; 
            letter++;

            if (!betterInterface)
            {
                BetterInterface();
            }

            if (cancelTyping)
            {
                typingSound.volume = 0;
                textSpeed = 0.000001f;
            }

            if (isChangingScene)
            {
                yield return new WaitForSeconds(1); //等待直到场景切换完成
                isChangingScene = false;
            }

            yield return new WaitForSeconds(textSpeed);
        }

        cancelTyping = false; //这样下一行开始时才会正常逐字读取
        textFinished = true; //当前行读取完毕
        textSpeed = tempTextSpeed; //回归正常播放速度
        littleArrow.color = new Vector4(0, 0, 0, 1); //小箭头

        if (auto)
        {
            yield return new WaitForSeconds(autoTextWaitingSpeed);
            nextLine = true;
        }

        index++;
    }
}