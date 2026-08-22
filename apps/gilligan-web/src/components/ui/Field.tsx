import type {
  InputHTMLAttributes,
  SelectHTMLAttributes,
  TextareaHTMLAttributes,
} from "react";

type BaseFieldProps = {
  error?: string;
  label: string;
  labelHidden?: boolean;
  required?: boolean;
};

type InputProps = BaseFieldProps & InputHTMLAttributes<HTMLInputElement>;

type SelectProps = BaseFieldProps &
  SelectHTMLAttributes<HTMLSelectElement> & {
    options: Array<{
      label: string;
      value: string;
    }>;
    placeholder?: string;
  };

type TextareaProps = BaseFieldProps & TextareaHTMLAttributes<HTMLTextAreaElement>;

export function Input({ error, label, labelHidden = false, required = false, className, ...props }: InputProps) {
  return (
    <label className="form-field">
      <FieldLabel hidden={labelHidden} label={label} required={required} />
      <input
        className={["form-control", error ? "form-control-invalid" : null, className].filter(Boolean).join(" ")}
        aria-invalid={Boolean(error)}
        {...props}
      />
      {error ? <span className="field-error">{error}</span> : null}
    </label>
  );
}

export function Select({
  error,
  label,
  labelHidden = false,
  options,
  placeholder,
  required = false,
  className,
  ...props
}: SelectProps) {
  return (
    <label className="form-field">
      <FieldLabel hidden={labelHidden} label={label} required={required} />
      <select
        className={["form-control", error ? "form-control-invalid" : null, className].filter(Boolean).join(" ")}
        aria-invalid={Boolean(error)}
        {...props}
      >
        {placeholder ? <option value="">{placeholder}</option> : null}
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {error ? <span className="field-error">{error}</span> : null}
    </label>
  );
}

export function Textarea({ error, label, labelHidden = false, required = false, className, ...props }: TextareaProps) {
  return (
    <label className="form-field form-field-full">
      <FieldLabel hidden={labelHidden} label={label} required={required} />
      <textarea
        className={["form-control", "form-textarea", error ? "form-control-invalid" : null, className]
          .filter(Boolean)
          .join(" ")}
        aria-invalid={Boolean(error)}
        {...props}
      />
      {error ? <span className="field-error">{error}</span> : null}
    </label>
  );
}

function FieldLabel({ hidden = false, label, required }: { hidden?: boolean; label: string; required: boolean }) {
  return (
    <span className={hidden ? "visually-hidden" : undefined}>
      {label}
      {required ? <span className="required-marker"> Required</span> : null}
    </span>
  );
}
