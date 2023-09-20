
<br>

# Suban 유니티 엑셀 데이터 매니저

유니티에서 엑셀 파일(xls, xlsx, xlsm)을 json으로 변환하고

게임에서 사용할 수 있도록 도와주는 패키지


<br>

## 엑셀 파일

![image](https://user-images.githubusercontent.com/96484044/224507767-5391b90f-3979-4ba1-93ed-c5425b88cf8b.png)

- 엑셀 파일은 프로젝트 최상단의 `_GameData` 폴더에 저장해야 함
- 엑셀의 시트마다 데이터 파일로 사용
    - 시트 이름의 앞에 `#` 을 추가하면 실제 데이터에 포함되지 않음
- `ExcelConverter.cs` 의 READ_SHEET define을 제거하면 엑셀의 첫번째 시트만 사용하게 됨
    - 첫번째 시트만 사용하는 경우 시트 이름은 상관 없으며 
    - 2번째 시트에 메모를 하는 식으로 활용 가능 (예 : Enum 목록)
- 시트의 1번째 행은 key 이름이 들어가야 함 (0번째 행에 메모를 하는 식으로 활용가능)
    - key이름 앞에 `#` 를 추가하면 실제 데이터에 포함되지 않음 

<br>

## ExcelConverter

![image](https://github.com/Super-Ba/SubanDataManager/assets/96484044/4d239414-453f-4c44-b7c2-da8fa4c35865)


- 엑셀 데이터를 Json으로 변환
- 엑셀데이터 수정 후 게임에 반영하기 위해 무조건 실행해야 함
- `GameData/Convert Excel Sheets` 메뉴를 통해 실행
- 변환된 파일은 `Assets/Data/GameData` 폴더에 저장
- NPOI 라이브러리를 사용함

json 배열의 0 번에는 데이터에 포함된 key 들 목록을 저장하고
1번 부터 실제 데이터가 들어감

앞에 `#`이 들어간 key는 json 내용에 추가하지 않음

<br>

## 데이터 매니저

게임 데이터들을 저장하고 관리하는 `IGameDataBase`와
데이터들이 들어있는 `IGameData`가 있음
그리고  `IGameDataBase`들은 `GameDataManager`에서 관리함

- **`IGameData`** : 데이터 Row 데이터들

- **`IGameDataBase`** : `IGameData` 목록을 저장하고 관리
    - **`GameDataBaseAttribute`** : 해당 데이터베이스가 관리하는 데이터의 타입과 json파일 경로를 연결함
    
- **`GameDataManager`** : `IGameDataBase`들을 초기화하고 관리함
    - `GameDataManager`에서  `IGameDataBase`을 저장할 때 (`Initialize()`) <br>
    
        모든 `IGameDataBase` 데이터 베이스와 `GameDataBaseAttribute`를 찾아
        필드의 타입에 맞게 데이터를 파싱하여 저장
    - 이때 `IGameData` 필드의 이름과 데이터의 Column 이름이 동일해야 하며
      `IGameData` 필드의 자료형을 찾아 파싱함
    - 파싱 가능한 타입은 `string`, `int`, `float`, `bool`, `Enum`(Flags)


<br>

## 예시

상단 엑셀구조와 동일 
```json
[
    [
        "Index",
        "Name",
        "Desc",
        "Value",
        "Bool",
        "Enum",
    ],
    [
        {
            "Index":"1",
            "Name":"테스트",
            "Desc":"이것은 설명",
            "Value":"3.14",
            "Bool":"TRUE",
            "Enum":"Option1",
        },
        {
            "Index":"2",
            "Name":"테스트2",
            "Desc":"설명은 이것",
            "Value":"6",
            "Bool":"FALSE",
            "Enum":"Option2",
        },
    ]
]
```

<br>

### `IGameData` 와 `IGameDataBase` 를 정의

```c#
public class TestGameData : IGameData
{
    public enum TestEnum
    {
        Option1,
        Option2,
    }

    public int Index { get; private set; }
    public string Name { get; private set; }
    public string Desc { get; private set; }
    public float Value { get; private set; }
    public bool Bool { get; private set; }
    public TestEnum Enum { get; private set; }
}


[GameDataBaseAttribute(typeof(TestGameData), "TestGameData")]
public class TestGameDataBase : IGameDataBase
{
    public Dictionary<int, IGameData> datas { get; set; }

    public TestGameData GetData(int id)
    {
        if (datas.TryGetValue(id, out var value))
        {
            return (TestGameData)value;
        }

        return null;
    }
}
```


<br>

### 사용 방법

```c#
var dataManager = new GameDataManager();
dataManager.Initialize();

var data = dataManager.GetDataBase<TestGameDataBase>().GetData(id);

Debug.Log(data.Name);
Debug.Log(data.Desc);
Debug.Log(data.Value);
Debug.Log(data.Bool);
Debug.Log(data.Enum);
```

<br>
<br>


## 데이터 검증기


![image](https://github.com/Super-Ba/SubanDataManager/assets/96484044/f2b7cdcd-fe1a-4496-99b4-3e0dd2d56ef3)

![image](https://github.com/Super-Ba/SubanDataManager/assets/96484044/ec083e4b-f61b-4077-b719-64ae4ac1509b)

엑셀 데이터 (json파일)의 컬럼과 GameData의 필드 이름을 비교하는 툴  

- 모든 Json컬럼과 GameData 필드 이름 표로 비교  
- Json컬럼에는 있지만 GameData의 필드 중 일치하는 것이 없는 경우 경고  
- GameData는 있지만 Json 파일이 없는 경우 경고  
ㅤ
먼저 `IGameDataBase` 들을 찾고 `GameDataBaseAttribute`의 `GameDataType`과 Json이름을 가져와 비교함


<br>
<br>

## 설치 방법

![image](https://github.com/Super-Ba/SubanDataManager/assets/96484044/b036266b-21bf-431f-9c3a-b236255568b7)
![image](https://github.com/Super-Ba/SubanDataManager/assets/96484044/540708ee-32f9-49b3-bf92-891564dd09d2)

유니티 패키지 매니저에서

`Add package from git URL` 선택 후 
 `https://github.com/Super-Ba/Suban-Unity-Excel-DataManager.git` 입력

<br>

## 레퍼런스

- 엑셀 파일을 읽기 위해  ([NPOI](https://github.com/dotnetcore/NPOI)) 라이브러리를 사용합니다
- Json 파일을 읽기 위해 ([NewtonsoftJson](https://www.newtonsoft.com/json)) 과 유니티의 ([Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@0.8/manual/index.html)) 를 사용합니다.
- 데이터 등록 과정 최적화를 위해 ([UniTask](https://github.com/Cysharp/UniTask)) 를 사용합니다.

<br>

## 라이선스

MIT 라이선스를 따릅니다

Apache License 2.0. 로 배포되는 라이브러리가 포함되어 있습니다. ([NPOI](https://github.com/dotnetcore/NPOI))
