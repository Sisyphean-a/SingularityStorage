<lane orientation="vertical" horizontal-content-alignment="center" margin="20">
    <!-- Title -->
    <label text="奇点存储 (Singularity Storage)" font="dialogue" margin="0, 0, 0, 20" />

    <!-- Search Bar -->
    <lane orientation="horizontal" vertical-content-alignment="middle" margin="0, 0, 0, 10">
        <label text="搜索: " margin="0, 0, 10, 0" />
        <text-input text="{SearchText}" width="300px" />
    </lane>

    <!-- Inventory Grid -->
    <scroll-container width="800px" height="500px">
        <grid item-width="64px" item-height="64px" horizontal-item-alignment="center" vertical-item-alignment="center">
            <lane *repeat={FilteredInventory} 
                  tooltip="{DisplayName}" 
                  focusable="true"
                  click="|OnItemClicked(this)|"> 
                <image sprite="{Sprite}" width="64px" height="64px" fit="contain" />
            </lane>
        </grid>
    </scroll-container>

    <!-- Loading Indicator -->
    <label text="加载中..." color="yellow" visible="{IsLoading}" />
</lane>
