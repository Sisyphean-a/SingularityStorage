<lane orientation="vertical" horizontal-content-alignment="middle" padding="20">
    <label text="奇点存储 (Singularity Storage)" font="dialogue" color="#331100" margin="0, 0, 0, 20" />

    <lane orientation="horizontal" vertical-content-alignment="middle" margin="0, 0, 0, 10">
        <label text="搜索: " color="#331100" margin="0, 0, 10, 0" />
        <textinput text={<>SearchText} layout="300px 54px" />
    </lane>

    <frame layout="800px 500px" background={@Mods/StardewUI/Sprites/ControlBorder} padding="16">
        <scrollable peeking="16">
            <grid layout="stretch content" 
                  item-layout="length: 64" 
                  item-spacing="8, 8" 
                  horizontal-item-alignment="middle">
                
                <image layout="64px 64px"
                       *repeat={FilteredInventory}
                       sprite={Sprite}
                       tooltip={DisplayName}
                       focusable="true" /> 
            </grid>
        </scrollable>
    </frame>

    <label text="加载中..." color="yellow" visibility={IsLoadingVisibility} margin="0, 10, 0, 0" />
    <label text={ItemCountText} color="#331100" margin="0, 10, 0, 0" />
</lane>