Dark form field for auth and profile screens. Focus glows blue; pass `error` to flip it red.

```jsx
<Input label="E-mail" type="email" placeholder="voce@email.com" leftIcon={<Mail size={18} />} />
<Input label="Senha" type="password" error="Senha muito curta" />
```

Controlled via `value` / `onChange`. `hint` shows helper text; `error` overrides it in danger color. `rightSlot` for a show/hide toggle or unit suffix.
