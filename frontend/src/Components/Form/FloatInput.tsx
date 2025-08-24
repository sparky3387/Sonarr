import React from "react";
import NumberInput from "./NumberInput";
import { InputChanged } from 'typings/inputs';

export interface FloatInputProps {
  name: string;
  value?: number | null;
  min?: number;
  max?: number;
  step?: number;
  placeholder?: string;
  className?: string;
  onChange: (change: InputChanged<number | null>) => void;
}

const FloatInput: React.FC<FloatInputProps> = (props) => {
  return (
    <NumberInput
      {...props}
      isFloat={true}
    />
  );
};

export default FloatInput;
