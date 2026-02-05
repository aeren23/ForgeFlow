
import re

def check_balance(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()

    stack = []
    lines = content.split('\n')
    
    for i, line in enumerate(lines):
        for j, char in enumerate(line):
            if char in '({[':
                stack.append((char, i + 1, j + 1))
            elif char in ')}]':
                if not stack:
                    print(f"Extra closing '{char}' at line {i+1} col {j+1}")
                    return
                
                last_open, last_line, last_col = stack.pop()
                expected = {'(': ')', '{': '}', '[': ']'}[last_open]
                if char != expected:
                    print(f"Mismatch: '{last_open}' at {last_line}:{last_col} closed by '{char}' at {i+1}:{j+1}")
                    return

    if stack:
        print("Unclosed items:")
        for item in stack:
            print(f"'{item[0]}' at {item[1]}:{item[2]}")
    else:
        print("Braces are balanced.")

check_balance(r"c:\Users\alihe\OneDrive\Masaüstü\ForgeFlow Project\frontend\src\features\projects\ProjectSettingsPage.tsx")
