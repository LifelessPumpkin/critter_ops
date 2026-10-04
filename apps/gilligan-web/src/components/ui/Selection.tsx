"use client";

import { createPortal } from "react-dom";
import {
  useEffect,
  useId,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type CSSProperties,
  type KeyboardEvent,
} from "react";
import { useDebouncedValue } from "@/components/activity/useDebouncedValue";

export type SelectionOption = {
  disabled?: boolean;
  label: string;
  value: string;
};

type SharedSelectionProps = {
  className?: string;
  disabled?: boolean;
  emptyMessage?: string;
  error?: string;
  label: string;
  labelHidden?: boolean;
  loading?: boolean;
  name?: string;
  options: SelectionOption[];
  placeholder?: string;
  required?: boolean;
};

type SelectProps = SharedSelectionProps & {
  onValueChange: (value: string) => void;
  value: string;
};

type ComboboxProps = SelectProps & {
  onSearchChange?: (value: string) => void;
  searchPlaceholder?: string;
};

type MultiSelectProps = SharedSelectionProps & {
  onValueChange: (value: string[]) => void;
  value: string[];
};

type Position = { above: boolean; left: number; top: number; width: number; maxHeight: number };

export function Select(props: SelectProps) {
  const { disabled = false, emptyMessage = "No options available.", error, label, labelHidden = false, loading = false, name, onValueChange, options, placeholder = "Select an option", required = false, value } = props;
  const [open, setOpen] = useState(false);
  const selectedIndex = options.findIndex((option) => option.value === value);
  const [activeIndex, setActiveIndex] = useState(Math.max(selectedIndex, firstEnabledIndex(options)));
  const selected = selectedIndex >= 0 ? options[selectedIndex] : undefined;
  const ids = useSelectionIds();
  const triggerRef = useRef<HTMLButtonElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);
  const position = useFloatingPosition(open, triggerRef);

  useDismiss(open, setOpen, triggerRef, contentRef);
  useEffect(() => {
    if (open) setActiveIndex(selectedIndex >= 0 ? selectedIndex : firstEnabledIndex(options));
  }, [open, options, selectedIndex]);

  function selectAt(index: number) {
    const option = options[index];
    if (!option || option.disabled) return;
    onValueChange(option.value);
    setOpen(false);
    window.requestAnimationFrame(() => triggerRef.current?.focus());
  }

  function handleKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
    if (disabled) return;
    if (!open && ["Enter", " ", "ArrowDown", "ArrowUp"].includes(event.key)) {
      event.preventDefault();
      setOpen(true);
      return;
    }
    if (!open) return;
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      setActiveIndex((current) => nextEnabledIndex(options, current, event.key === "ArrowDown" ? 1 : -1));
    } else if (event.key === "Home" || event.key === "End") {
      event.preventDefault();
      setActiveIndex(event.key === "Home" ? firstEnabledIndex(options) : lastEnabledIndex(options));
    } else if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      selectAt(activeIndex);
    }
  }

  return (
    <SelectionField label={label} labelHidden={labelHidden} required={required} error={error} ids={ids}>
      {name ? <input type="hidden" name={name} value={value} /> : null}
      <button
        ref={triggerRef}
        id={ids.controlId}
        type="button"
        className={triggerClassName(props.className, error, open)}
        role="combobox"
        aria-activedescendant={open && options[activeIndex] ? `${ids.optionPrefix}-${activeIndex}` : undefined}
        aria-controls={ids.listboxId}
        aria-describedby={error ? ids.errorId : undefined}
        aria-expanded={open}
        aria-haspopup="listbox"
        aria-invalid={Boolean(error)}
        aria-labelledby={`${ids.labelId} ${ids.valueId}`}
        aria-required={required}
        disabled={disabled}
        onClick={() => setOpen((current) => !current)}
        onKeyDown={handleKeyDown}
      >
        <span id={ids.valueId} className={selected ? undefined : "selection-placeholder"}>{selected?.label ?? placeholder}</span>
        <Chevron open={open} />
      </button>
      {open ? createPortal(
        <SelectionListbox ids={ids} options={options} activeIndex={activeIndex} selectedValues={value ? [value] : []} position={position} contentRef={contentRef} loading={loading} emptyMessage={emptyMessage} onHover={setActiveIndex} onSelect={selectAt} />,
        getPortalRoot(triggerRef.current),
      ) : null}
    </SelectionField>
  );
}

export function Combobox(props: ComboboxProps) {
  const { disabled = false, emptyMessage = "No matches found.", error, label, labelHidden = false, loading = false, name, onSearchChange, onValueChange, options, placeholder = "Search or select...", required = false, searchPlaceholder = "Search...", value } = props;
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const debouncedSearch = useDebouncedValue(search, 300);
  const selected = options.find((option) => option.value === value);
  const filteredOptions = useMemo(() => {
    if (onSearchChange || !search.trim()) return options;
    const term = search.trim().toLocaleLowerCase();
    return options.filter((option) => option.label.toLocaleLowerCase().includes(term));
  }, [onSearchChange, options, search]);
  const ids = useSelectionIds();
  const triggerRef = useRef<HTMLButtonElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);
  const position = useFloatingPosition(open, triggerRef);

  useDismiss(open, setOpen, triggerRef, contentRef);
  useEffect(() => { if (onSearchChange) onSearchChange(debouncedSearch); }, [debouncedSearch, onSearchChange]);
  useEffect(() => {
    if (!open) { setSearch(""); return; }
    setActiveIndex(firstEnabledIndex(filteredOptions));
    window.requestAnimationFrame(() => searchRef.current?.focus());
  }, [filteredOptions, open]);

  function selectAt(index: number) {
    const option = filteredOptions[index];
    if (!option || option.disabled) return;
    onValueChange(option.value);
    setOpen(false);
    window.requestAnimationFrame(() => triggerRef.current?.focus());
  }

  function handleSearchKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      setActiveIndex((current) => nextEnabledIndex(filteredOptions, current, event.key === "ArrowDown" ? 1 : -1));
    } else if (event.key === "Enter") {
      event.preventDefault();
      selectAt(activeIndex);
    } else if (event.key === "Escape") {
      event.preventDefault();
      setOpen(false);
      triggerRef.current?.focus();
    } else if (event.key === "Tab") {
      event.preventDefault();
      setOpen(false);
      focusAdjacent(triggerRef.current, event.shiftKey ? -1 : 1);
    }
  }

  return (
    <SelectionField label={label} labelHidden={labelHidden} required={required} error={error} ids={ids}>
      {name ? <input type="hidden" name={name} value={value} /> : null}
      <button ref={triggerRef} id={ids.controlId} type="button" className={triggerClassName(props.className, error, open)} role="combobox" aria-controls={ids.listboxId} aria-describedby={error ? ids.errorId : undefined} aria-expanded={open} aria-haspopup="listbox" aria-invalid={Boolean(error)} aria-labelledby={`${ids.labelId} ${ids.valueId}`} aria-required={required} disabled={disabled} onClick={() => setOpen((current) => !current)} onKeyDown={(event) => { if (["Enter", " ", "ArrowDown", "ArrowUp"].includes(event.key)) { event.preventDefault(); setOpen(true); } }}>
        <span id={ids.valueId} className={selected ? undefined : "selection-placeholder"}>{selected?.label ?? placeholder}</span><Chevron open={open} />
      </button>
      {open ? createPortal(
        <div ref={contentRef} className="selection-content" style={positionStyle(position)}>
          <label className="selection-search"><span className="visually-hidden">{searchPlaceholder}</span><span aria-hidden="true">⌕</span><input ref={searchRef} value={search} placeholder={searchPlaceholder} role="searchbox" aria-controls={ids.listboxId} aria-activedescendant={filteredOptions[activeIndex] ? `${ids.optionPrefix}-${activeIndex}` : undefined} onChange={(event) => setSearch(event.target.value)} onKeyDown={handleSearchKeyDown} /></label>
          <SelectionOptions ids={ids} options={filteredOptions} activeIndex={activeIndex} selectedValues={value ? [value] : []} loading={loading} emptyMessage={emptyMessage} onHover={setActiveIndex} onSelect={selectAt} />
        </div>,
        getPortalRoot(triggerRef.current),
      ) : null}
    </SelectionField>
  );
}

export function MultiSelect(props: MultiSelectProps) {
  const { disabled = false, emptyMessage = "No options available.", error, label, labelHidden = false, loading = false, onValueChange, options, placeholder, required = false, value } = props;
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(firstEnabledIndex(options));
  const ids = useSelectionIds();
  const triggerRef = useRef<HTMLButtonElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);
  const position = useFloatingPosition(open, triggerRef);
  useDismiss(open, setOpen, triggerRef, contentRef);

  function toggleAt(index: number) {
    const option = options[index];
    if (!option || option.disabled) return;
    onValueChange(value.includes(option.value) ? value.filter((item) => item !== option.value) : [...value, option.value]);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
    if (!open && ["Enter", " ", "ArrowDown", "ArrowUp"].includes(event.key)) { event.preventDefault(); setOpen(true); return; }
    if (!open) return;
    if (event.key === "ArrowDown" || event.key === "ArrowUp") { event.preventDefault(); setActiveIndex((current) => nextEnabledIndex(options, current, event.key === "ArrowDown" ? 1 : -1)); }
    else if (event.key === "Enter" || event.key === " ") { event.preventDefault(); toggleAt(activeIndex); }
    else if (event.key === "Home" || event.key === "End") { event.preventDefault(); setActiveIndex(event.key === "Home" ? firstEnabledIndex(options) : lastEnabledIndex(options)); }
  }

  const displayValue = value.length ? `${label} (${value.length})` : placeholder ?? label;
  return (
    <SelectionField label={label} labelHidden={labelHidden} required={required} error={error} ids={ids} compact>
      <button ref={triggerRef} id={ids.controlId} type="button" className={triggerClassName(props.className, error, open)} role="combobox" aria-activedescendant={open && options[activeIndex] ? `${ids.optionPrefix}-${activeIndex}` : undefined} aria-controls={ids.listboxId} aria-describedby={error ? ids.errorId : undefined} aria-expanded={open} aria-haspopup="listbox" aria-invalid={Boolean(error)} aria-labelledby={`${ids.labelId} ${ids.valueId}`} disabled={disabled} onClick={() => setOpen((current) => !current)} onKeyDown={handleKeyDown}>
        <span id={ids.valueId}>{displayValue}</span><Chevron open={open} />
      </button>
      {open ? createPortal(<SelectionListbox ids={ids} options={options} activeIndex={activeIndex} selectedValues={value} position={position} contentRef={contentRef} loading={loading} emptyMessage={emptyMessage} onHover={setActiveIndex} onSelect={toggleAt} multi />, getPortalRoot(triggerRef.current)) : null}
    </SelectionField>
  );
}

function SelectionField({ children, compact = false, error, ids, label, labelHidden, required }: { children: React.ReactNode; compact?: boolean; error?: string; ids: ReturnType<typeof useSelectionIds>; label: string; labelHidden: boolean; required: boolean }) {
  return <div className={compact ? "selection-field selection-field-compact" : "form-field selection-field"}><span id={ids.labelId} className={labelHidden ? "visually-hidden" : undefined}>{label}{required ? <span className="required-marker"> Required</span> : null}</span>{children}{error ? <span id={ids.errorId} className="field-error">{error}</span> : null}</div>;
}

function SelectionListbox({ position, contentRef, ...props }: Parameters<typeof SelectionOptions>[0] & { position: Position; contentRef: React.RefObject<HTMLDivElement | null> }) {
  return <div ref={contentRef} className="selection-content" style={positionStyle(position)}><SelectionOptions {...props} /></div>;
}

function SelectionOptions({ activeIndex, emptyMessage, ids, loading, multi = false, onHover, onSelect, options, selectedValues }: { activeIndex: number; emptyMessage: string; ids: ReturnType<typeof useSelectionIds>; loading: boolean; multi?: boolean; onHover: (index: number) => void; onSelect: (index: number) => void; options: SelectionOption[]; selectedValues: string[] }) {
  return (
    <div id={ids.listboxId} className="selection-listbox" role="listbox" aria-multiselectable={multi || undefined} aria-busy={loading}>
      {loading ? <div className="selection-state"><span className="selection-spinner" aria-hidden="true" /> Loading options…</div> : options.length ? options.map((option, index) => {
        const selected = selectedValues.includes(option.value);
        return <div key={option.value} id={`${ids.optionPrefix}-${index}`} className={["selection-option", index === activeIndex ? "selection-option-active" : null, selected ? "selection-option-selected" : null, option.disabled ? "selection-option-disabled" : null].filter(Boolean).join(" ")} role="option" aria-disabled={option.disabled || undefined} aria-selected={selected} onMouseDown={(event) => { event.preventDefault(); onSelect(index); }} onMouseEnter={() => onHover(index)}><span>{option.label}</span><span className="selection-check" aria-hidden="true">{selected ? "✓" : ""}</span></div>;
      }) : <div className="selection-state">{emptyMessage}</div>}
    </div>
  );
}

function Chevron({ open }: { open: boolean }) { return <span className={open ? "selection-chevron selection-chevron-open" : "selection-chevron"} aria-hidden="true">⌄</span>; }
function triggerClassName(className?: string, error?: string, open?: boolean) { return ["form-control", "selection-trigger", className, error ? "form-control-invalid" : null, open ? "selection-trigger-open" : null].filter(Boolean).join(" "); }
function positionStyle(position: Position): CSSProperties { return { left: position.left, top: position.top, width: position.width, maxHeight: position.maxHeight, transform: position.above ? "translateY(-100%)" : undefined }; }

function useSelectionIds() {
  const id = useId().replace(/:/g, "");
  return { controlId: `selection-${id}`, errorId: `selection-${id}-error`, labelId: `selection-${id}-label`, listboxId: `selection-${id}-listbox`, optionPrefix: `selection-${id}-option`, valueId: `selection-${id}-value` };
}

function useDismiss(open: boolean, setOpen: (open: boolean) => void, triggerRef: React.RefObject<HTMLElement | null>, contentRef: React.RefObject<HTMLElement | null>) {
  useEffect(() => {
    if (!open) return;
    function pointerDown(event: PointerEvent) { const target = event.target as Node; if (!triggerRef.current?.contains(target) && !contentRef.current?.contains(target)) setOpen(false); }
    function keyDown(event: globalThis.KeyboardEvent) { if (event.key === "Escape") { setOpen(false); triggerRef.current?.focus(); } }
    document.addEventListener("pointerdown", pointerDown);
    document.addEventListener("keydown", keyDown);
    return () => { document.removeEventListener("pointerdown", pointerDown); document.removeEventListener("keydown", keyDown); };
  }, [contentRef, open, setOpen, triggerRef]);
}

function useFloatingPosition(open: boolean, triggerRef: React.RefObject<HTMLElement | null>) {
  const [position, setPosition] = useState<Position>({ above: false, left: 0, top: 0, width: 240, maxHeight: 280 });
  useLayoutEffect(() => {
    if (!open || !triggerRef.current) return;
    function update() {
      const rect = triggerRef.current!.getBoundingClientRect();
      const width = Math.min(Math.max(rect.width, 200), window.innerWidth - 16);
      const left = Math.min(Math.max(8, rect.left), window.innerWidth - width - 8);
      const below = window.innerHeight - rect.bottom - 8;
      const above = rect.top - 8;
      const placeAbove = below < 180 && above > below;
      const maxHeight = Math.min(320, Math.max(120, placeAbove ? above : below));
      setPosition({ above: placeAbove, left, top: placeAbove ? rect.top - 4 : rect.bottom + 4, width, maxHeight });
    }
    update();
    window.addEventListener("resize", update);
    window.addEventListener("scroll", update, true);
    return () => { window.removeEventListener("resize", update); window.removeEventListener("scroll", update, true); };
  }, [open, triggerRef]);
  return position;
}

function firstEnabledIndex(options: SelectionOption[]) { return Math.max(0, options.findIndex((option) => !option.disabled)); }
function lastEnabledIndex(options: SelectionOption[]) { for (let index = options.length - 1; index >= 0; index -= 1) if (!options[index].disabled) return index; return 0; }
function nextEnabledIndex(options: SelectionOption[], current: number, direction: 1 | -1) { if (!options.length) return 0; let index = current; for (let attempts = 0; attempts < options.length; attempts += 1) { index = (index + direction + options.length) % options.length; if (!options[index].disabled) return index; } return current; }
function focusAdjacent(trigger: HTMLElement | null, direction: 1 | -1) { if (!trigger) return; const focusable = Array.from(document.querySelectorAll<HTMLElement>('button:not([disabled]), input:not([disabled]), textarea:not([disabled]), [href], [tabindex]:not([tabindex="-1"])')).filter((element) => element.offsetParent !== null && !element.closest(".selection-content")); const index = focusable.indexOf(trigger); focusable[index + direction]?.focus(); }
function getPortalRoot(trigger: HTMLElement | null) { return trigger?.closest("dialog") ?? document.body; }
