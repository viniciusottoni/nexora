Hunter portrait. Give it a `rank` to wrap it in that rank's glowing ring — used on the profile card and leaderboards.

```jsx
<Avatar src={user.photo} name="Kael Voss" rank="S" size={72} />
<Avatar name="Ana Reis" size={40} online />
```

No `src` → initials on the energy gradient. `size` drives everything (ring, dot, type) proportionally.
