// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AntDesign.Select.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AntDesign.JsInterop;
using OneOf;
using System.Linq;

namespace AntDesign
{
    public partial class TableSelect<TItem> : SelectBase<string, TItem> where TItem : class
    {
        [Parameter] public bool ShowExpand { get; set; } = true;

        [Parameter] public bool Multiple { get; set; }

        [Parameter] public bool TreeCheckable { get; set; }

        [Parameter] public string PopupContainerSelector { get; set; } = "body";

        [Parameter] public Action OnMouseEnter { get; set; }

        [Parameter] public Action OnMouseLeave { get; set; }

        [Parameter] public Action OnBlur { get; set; }

        [Parameter] public IEnumerable<TItem> DataSource { get; set; }

        [Parameter] public Func<TreeNode<TItem>, bool> SearchExpression { get; set; }

        [Parameter] public Func<TItem, object> RowKey { get; set; }

        [Parameter] public EventCallback<string> OnSearch { get; set; }

        [Parameter] public OneOf<bool, string> DropdownMatchSelectWidth { get; set; } = true;

        [Parameter] public string DropdownMaxWidth { get; set; } = "auto";

        [Parameter] public string PopupContainerMaxHeight { get; set; } = "256px";

        [Parameter] public string DropdownStyle { get; set; }

        private const string ClassPrefix = "ant-select";

        internal override SelectMode SelectMode => Multiple ? SelectMode.Multiple : base.SelectMode;

        private string[] SelectedKeys => Values?.ToArray();

        private string _dropdownStyle = string.Empty;

        public override string Value
        {
            get => base.Value;
            set
            {
                if (base.Value == value)
                    return;

                base.Value = value;

                if (value == null)
                {
                    ClearOptions();
                }

                UpdateValueAndSelection();
            }
        }

        public override IEnumerable<string> Values
        {
            get => base.Values;
            set
            {
                if (value != null && _selectedValues != null)
                {
                    var hasChanged = !value.SequenceEqual(_selectedValues);

                    if (!hasChanged)
                        return;

                    _selectedValues = value;
                }
                else if (value != null && _selectedValues == null)
                {
                    _selectedValues = value;
                }
                else if (value == null && _selectedValues != null)
                {
                    _selectedValues = default;
                    ClearOptions();
                }

                UpdateValuesSelection();

                if (_isNotifyFieldChanged && (Form?.ValidateOnChange == true))
                {
                    EditContext?.NotifyFieldChanged(FieldIdentifier);
                }
            }
        }

        private void ClearOptions()
        {
            SelectOptionItems.Clear();
            SelectedOptionItems.Clear();
            //_tree?._allNodes.ForEach(x => x.SetSelected(false));
        }

        private void CreateOptions(IEnumerable<string> data)
        {
        }

        private void OnKeyDownAsync(KeyboardEventArgs args)
        {
        }

        protected async void OnInputAsync(ChangeEventArgs e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (!IsSearchEnabled)
            {
                return;
            }

            if (!_dropDown.IsOverlayShow())
            {
                await _dropDown.Show();
            }

            _prevSearchValue = _searchValue;

            if (OnSearch.HasDelegate)
                await OnSearch.InvokeAsync(e.Value?.ToString());

            _searchValue = e.Value?.ToString();
            StateHasChanged();
        }

        protected async Task OnKeyUpAsync(KeyboardEventArgs e)
        {
        }

        protected async Task OnInputFocusAsync(FocusEventArgs _)
        {
            await SetInputFocusAsync();
        }

        protected async Task OnInputBlurAsync(FocusEventArgs _)
        {
            await SetInputBlurAsync();
        }

        protected async Task SetInputBlurAsync()
        {
            if (Focused)
            {
                Focused = false;

                SetClassMap();

                await JsInvokeAsync(JSInteropConstants.Blur, _inputRef);

                OnBlur?.Invoke();
            }
        }

        private async Task OnOverlayVisibleChangeAsync(bool visible)
        {
            if (visible)
            {
                await SetDropdownStyleAsync();

                await SetInputFocusAsync();
            }
            else
            {
                OnOverlayHide();
            }
        }

        protected async Task OnRemoveSelectedAsync(SelectOptionItem<string, TItem> selectOption)
        {
            if (selectOption == null) throw new ArgumentNullException(nameof(selectOption));
            await SetValueAsync(selectOption);

            foreach (var item in DataSource.Select(x => RowKey(x)).ToList())
            {
                //if (RowKey(item).Equals(selectOption.Value))
                //item.SetSelected(false);
            }
        }

        protected async Task SetDropdownStyleAsync()
        {
            string maxWidth = "", minWidth = "", definedWidth = "";
            var domRect = await JsInvokeAsync<DomRect>(JSInteropConstants.GetBoundingClientRect, Ref);
            var width = domRect.Width.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            minWidth = $"min-width: {width}px;";
            if (DropdownMatchSelectWidth.IsT0 && DropdownMatchSelectWidth.AsT0)
            {
                definedWidth = $"width: {width}px;";
            }
            else if (DropdownMatchSelectWidth.IsT1)
            {
                definedWidth = $"width: {DropdownMatchSelectWidth.AsT1};";
            }
            if (!DropdownMaxWidth.Equals("auto", StringComparison.CurrentCultureIgnoreCase))
                maxWidth = $"max-width: {DropdownMaxWidth};";
            _dropdownStyle = minWidth + definedWidth + maxWidth + DropdownStyle ?? "";

            if (Multiple)
            {
                if (_selectedValues == null)
                    return;
                // DataSource.ForEach(n => n.SetSelected(_selectedValues.Contains(n.Key)));
            }
            else
            {
                //DataSource?.FindFirstOrDefaultNode(node => node.Key == Value)?.SetSelected(true);
            }
        }

        protected override void SetClassMap()
        {
            ClassMapper
                .Add("ant-tree-select")
                .Add($"{ClassPrefix}")
                .If($"{ClassPrefix}-open", () => _dropDown?.IsOverlayShow() ?? false)
                .If($"{ClassPrefix}-focused", () => Focused)
                .If($"{ClassPrefix}-single", () => SelectMode == SelectMode.Default)
                .If($"{ClassPrefix}-multiple", () => SelectMode != SelectMode.Default)
                .If($"{ClassPrefix}-sm", () => Size == AntSizeLDSType.Small)
                .If($"{ClassPrefix}-lg", () => Size == AntSizeLDSType.Large)
                .If($"{ClassPrefix}-show-arrow", () => ShowArrowIcon)
                .If($"{ClassPrefix}-show-search", () => IsSearchEnabled)
                .If($"{ClassPrefix}-loading", () => Loading)
                .If($"{ClassPrefix}-disabled", () => Disabled)
                .If($"{ClassPrefix}-rtl", () => RTL)
                .If($"{ClassPrefix}-allow-clear", () => AllowClear)
                ;
        }

        private void UpdateValueAndSelection()
        {
            if (SelectOptionItems.Any(o => o.Value == Value))
            {
                _ = SetValueAsync(SelectOptionItems.First(o => o.Value == Value));
            }
            else
            {
                //var data = _tree?._allNodes.FirstOrDefault(x => x.Key == Value);
                //if (data != null)
                //{
                //    //var o = CreateOption(data, true);
                //    _ = SetValueAsync(o);
                //}
            }
        }

        private void UpdateValuesSelection()
        {
            if (_selectedValues?.Any() != true)
            {
                ClearOptions();
            }

            CreateOptions(_selectedValues);
            _ = OnValuesChangeAsync(_selectedValues);
        }
    }
}
