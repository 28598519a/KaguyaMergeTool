# KaguyaMerge

### Since the CG merge definition has been parsed from params.dat, a new repo will be opened with this guessing version of KaguyaMerge will be deprecated
### This is new version of [KaguyaMerge](https://github.com/28598519a/KaguyaMerge)

用於自動化合成Atelier Kaguya的CG (目前約20%的CG仍需手動合成)

如果沒有一次合成到底的話，建議最好是把Used、Usedanm(backup)裡面的檔案重新拿出來，刪掉CG資料夾，整個重合

由於需要拿到XY Offset table，因此需要配合這個專案的程式使用，https://github.com/28598519a/GetKaguyaXY
輸出的 AlpXY_Offset(Auto).txt 就是給這個程式用的Offset表

=> Test on: [Atelier KAGUYA] LOVEトレ

目前有2類會整組放棄合成，需要手動，如果有人有總結出合成規則，可以開個issue
1. 含有多個CG底圖的 (正常是CGXXX，而多個的是CGXXX_1、CGXXX_2...)，因為不知道要用哪個底圖去合差分圖(甲)
2. 含有 部 字樣的差分圖，由於跟差分圖(甲)的對應規則沒有總結出來，連帶只好整組CG放棄掉
