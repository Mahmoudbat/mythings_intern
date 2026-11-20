# MyThings — Database Sketch

Schema sketch for the customer journey (B05), sections 1–10: users, addresses, stores, the menu, the cart, orders and drivers.
Written in SQL Server (T-SQL) style. This is a design sketch, not a final migration — with Code First the real tables will come from the C# entities.

## Assumptions

- The backend is a stateless REST API, so anything that has to survive between requests (e.g. the last used address) is stored in the database.
- Values that can be derived with a query are **not** stored as columns:
  - whether a user has orders in some category → select from the orders table
  - whether a store has at least one product for sale → select from `menu_item`
  - whether a store is open right now → computed at query time from `opens_at` / `closes_at` (Amman time) and `is_paused`

## Tables

### users

`user` is a reserved word, so the table is called `users`.

```sql
CREATE TABLE users (
    id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    first_name            NVARCHAR(50)  NOT NULL,
    last_name             NVARCHAR(50)  NOT NULL,
    phone_num             VARCHAR(20)   NOT NULL UNIQUE,   -- one phone number = one account
    gender                VARCHAR(10)   NOT NULL CHECK (gender IN ('male', 'female')),
    last_used_address_id  UNIQUEIDENTIFIER NULL,           -- FK added after address exists (see below)
    created_at            DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    updated_at            DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);
```

- `last_used_address_id` lets the app pick the last used address automatically. It could later become "most used address" instead — a business decision.

### address

A customer can save many addresses, so addresses get their own table.

```sql
CREATE TABLE address (
    id               UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    user_id          UNIQUEIDENTIFIER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name             NVARCHAR(50)   NOT NULL,   -- "Home", "Work"
    city             NVARCHAR(50)   NOT NULL,
    area             NVARCHAR(50)   NOT NULL,
    street           NVARCHAR(100)  NOT NULL,
    building_number  NVARCHAR(10)   NOT NULL,
    floor            INT            NOT NULL,
    note             NVARCHAR(255)  NULL,       -- note for the driver (optional)
    latitude         DECIMAL(9,6)   NOT NULL,
    longitude        DECIMAL(9,6)   NOT NULL,
    created_at       DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    updated_at       DATETIME2      NOT NULL DEFAULT SYSDATETIME()
);

-- users <-> address reference each other, so this FK is added after both tables exist
ALTER TABLE users
    ADD CONSTRAINT fk_users_last_used_address
    FOREIGN KEY (last_used_address_id) REFERENCES address(id);
```

- `ON DELETE CASCADE`: when a user deletes their account, their saved addresses are deleted too.

### store

```sql
CREATE TABLE store (
    id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    owner_id      UNIQUEIDENTIFIER NOT NULL REFERENCES users(id),
    name          NVARCHAR(100)  NOT NULL,
    category      VARCHAR(20)    NOT NULL CHECK (category IN ('restaurant', 'groceries', 'cafe')),
    is_paused     BIT            NOT NULL DEFAULT 0,   -- store paused new orders (too busy)
    opens_at      TIME           NOT NULL,
    closes_at     TIME           NOT NULL,
    delivery_fee  DECIMAL(10,3)  NOT NULL DEFAULT 0 CHECK (delivery_fee >= 0),
    city          NVARCHAR(50)   NOT NULL,
    street_name   NVARCHAR(100),
    latitude      DECIMAL(9,6)   NOT NULL,
    longitude     DECIMAL(9,6)   NOT NULL,
    created_at    DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    updated_at    DATETIME2      NOT NULL DEFAULT SYSDATETIME()
);
```

- Assumption: one person can own multiple stores.
- Deleting a user who owns a store fails because of `owner_id` (no cascade). This is on purpose: deleting an account shouldn't wipe a store.
- The store keeps its own location instead of referencing `address`, which is for customers only.
- SQL Server can sort by distance, so "closest store first" can be done in the query.

### section

Menu sections ("Burgers", "Drinks"). Each section belongs to one store — they are not global.

```sql
CREATE TABLE section (
    id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    store_id    UNIQUEIDENTIFIER NOT NULL REFERENCES store(id) ON DELETE CASCADE,
    name        NVARCHAR(50)  NOT NULL,
    sort_order  INT           NOT NULL DEFAULT 0,   -- display order in the app
    created_at  DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    updated_at  DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);
```

### menu_item

```sql
CREATE TABLE menu_item (
    id           UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    section_id   UNIQUEIDENTIFIER NOT NULL REFERENCES section(id) ON DELETE CASCADE,
    name         NVARCHAR(100)  NOT NULL,
    description  NVARCHAR(MAX),
    price        DECIMAL(10,3)  NOT NULL CHECK (price >= 0),
    is_sold_out  BIT            NOT NULL DEFAULT 0,   -- still visible, but can't be added to cart
    created_at   DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    updated_at   DATETIME2      NOT NULL DEFAULT SYSDATETIME()
);
```

- The store is reached through the section, so there is no `store_id` here.

### option_group

Groups like Size, Extras and Sauce. A group belongs to the **store**, so one group (e.g. Sauce) can be shared by many items.

```sql
CREATE TABLE option_group (
    id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    store_id    UNIQUEIDENTIFIER NOT NULL REFERENCES store(id) ON DELETE CASCADE,
    name        NVARCHAR(50)  NOT NULL,
    min_select  INT           NOT NULL,   -- Size: 1, Extras: 0, Sauce: 0
    max_select  INT           NOT NULL,   -- Size: 1, Extras: 3, Sauce: 2
    created_at  DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    updated_at  DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CHECK (min_select >= 0 AND max_select >= min_select)
);
```

### menu_option

The choices inside a group ("Large", "Cheese", "Garlic").

```sql
CREATE TABLE menu_option (
    id               UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    option_group_id  UNIQUEIDENTIFIER NOT NULL REFERENCES option_group(id) ON DELETE CASCADE,
    name             NVARCHAR(50)   NOT NULL,
    extra_price      DECIMAL(10,3)  NOT NULL CHECK (extra_price >= 0),   -- added to the item price
    created_at       DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    updated_at       DATETIME2      NOT NULL DEFAULT SYSDATETIME()
);
```

### menu_item_option_group

Join table: which option groups show on which menu item (many-to-many).

```sql
CREATE TABLE menu_item_option_group (
    id               UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    menu_item_id     UNIQUEIDENTIFIER NOT NULL REFERENCES menu_item(id) ON DELETE CASCADE,
    option_group_id  UNIQUEIDENTIFIER NOT NULL REFERENCES option_group(id),   -- no cascade, see backend notes
    UNIQUE (menu_item_id, option_group_id)
);
```

### cart

One cart per user (spec 5: one store per cart, "Empty your cart and start a new one?"). The cart lives in the database, so its the same on every device.

```sql
CREATE TABLE cart (
    id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    user_id     UNIQUEIDENTIFIER NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,   -- UNIQUE = one cart per user
    store_id    UNIQUEIDENTIFIER NOT NULL REFERENCES store(id),   -- no cascade, see backend notes
    created_at  DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    updated_at  DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
```

- The cart row is created when the first item is added, and deleted when the user empties it or places the order ("my cart is empty again").
- Open question for the trainer: one cart per user (the spec) or one per store category (the real app). Per category would be `UNIQUE (user_id, category)` instead.

### cart_item

```sql
CREATE TABLE cart_item (
    id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    cart_id       UNIQUEIDENTIFIER NOT NULL REFERENCES cart(id) ON DELETE CASCADE,
    menu_item_id  UNIQUEIDENTIFIER NOT NULL REFERENCES menu_item(id) ON DELETE CASCADE,
    quantity      INT            NOT NULL CHECK (quantity > 0),
    note          NVARCHAR(255)  NULL,   -- "no onions"
    created_at    DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    updated_at    DATETIME2      NOT NULL DEFAULT SYSDATETIME()
);
```

- No `UNIQUE (cart_id, menu_item_id)` on purpose: the same burger with different options or a different note is a separate line. Same item + same options + same note → the backend bumps `quantity` instead of adding a line.
- No price here: the cart always shows the current price from `menu_item` (spec 5).
- No `user_id` / `store_id` here: they come through `cart_id`.

### cart_item_option

The options chosen for one cart line. A table instead of a JSON list, so the FKs and the UNIQUE still work.

```sql
CREATE TABLE cart_item_option (
    id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    cart_item_id    UNIQUEIDENTIFIER NOT NULL REFERENCES cart_item(id) ON DELETE CASCADE,
    menu_option_id  UNIQUEIDENTIFIER NOT NULL REFERENCES menu_option(id),   -- no cascade, see backend notes
    UNIQUE (cart_item_id, menu_option_id)
);
```

### driver

A driver is a user (they sign up as a customer first), so name and phone come from `users`. This table only holds driver-only data. `user_id` is both the PK and the FK, which makes it 1:1: a user can be a driver at most once.

```sql
CREATE TABLE driver (
    user_id     UNIQUEIDENTIFIER PRIMARY KEY REFERENCES users(id),   -- no cascade: drivers stay in order history
    is_active   BIT       NOT NULL DEFAULT 0,
    created_at  DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    updated_at  DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
```

### orders

`order` is a reserved word, so the table is called `orders`. Placing an order copies everything out of the cart (Option B in Business decisions), so the order never depends on the cart, the menu or the saved address again.

```sql
CREATE TABLE orders (
    id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    user_id               UNIQUEIDENTIFIER NOT NULL REFERENCES users(id),
    store_id              UNIQUEIDENTIFIER NULL REFERENCES store(id) ON DELETE SET NULL,
    driver_id             UNIQUEIDENTIFIER NULL REFERENCES driver(user_id),   -- NULL until a driver takes it

    -- snapshots, copied at "Place order"
    store_name            NVARCHAR(100)  NOT NULL,
    address_name          NVARCHAR(50)   NOT NULL,
    city                  NVARCHAR(50)   NOT NULL,
    area                  NVARCHAR(50)   NOT NULL,
    street                NVARCHAR(100)  NOT NULL,
    building_number       NVARCHAR(10)   NOT NULL,
    floor                 INT            NOT NULL,
    address_note          NVARCHAR(255)  NULL,
    latitude              DECIMAL(9,6)   NOT NULL,
    longitude             DECIMAL(9,6)   NOT NULL,
    delivery_fee          DECIMAL(10,3)  NOT NULL CHECK (delivery_fee >= 0),
    total                 DECIMAL(10,3)  NOT NULL CHECK (total >= 0),   -- lines + delivery fee

    note                  NVARCHAR(255)  NULL,   -- "Please ring the bell twice."
    payment_method        VARCHAR(10)    NOT NULL CHECK (payment_method IN ('cash', 'card')),
    status                VARCHAR(20)    NOT NULL DEFAULT 'placed'
                          CHECK (status IN ('placed', 'accepted', 'on_the_way', 'delivered', 'declined', 'cancelled')),

    -- when each step happened (created_at = placed)
    accepted_at           DATETIME2      NULL,
    on_the_way_at         DATETIME2      NULL,
    delivered_at          DATETIME2      NULL,
    declined_at           DATETIME2      NULL,
    cancelled_at          DATETIME2      NULL,
    cancellation_reason   NVARCHAR(255)  NULL,   -- why it was declined or cancelled

    created_at            DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    updated_at            DATETIME2      NOT NULL DEFAULT SYSDATETIME(),

    -- declined/cancelled orders must have a reason; other orders must not
    CHECK (
        (status IN ('declined', 'cancelled') AND cancellation_reason IS NOT NULL)
        OR (status NOT IN ('declined', 'cancelled') AND cancellation_reason IS NULL)
    )
);
```

- Why not `cart_id`: the cart is emptied (deleted) right after the order is placed, and with one cart per user every order would point at the same cart. The order needs its own `user_id` and `store_id` instead.
- `cancellation_reason` is NULL for normal orders. The CHECK forces a reason exactly when the status is declined or cancelled.
- `store_id` is `SET NULL` so a store can still be deleted (Option B). The history keeps showing `store_name`.
- Deleting a user who has orders fails (no cascade on `user_id`), same as store owners. Deleting or anonymizing old orders is a business decision.
- Declined = ended before a driver took it, cancelled = ended after (spec 7). The backend sets the matching `*_at` column with the status.

### order_item

One line of an order, with copied values (spec 10: the old price and the removed Chicken Sandwich still show).

```sql
CREATE TABLE order_item (
    id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    order_id      UNIQUEIDENTIFIER NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    menu_item_id  UNIQUEIDENTIFIER NULL,   -- plain copy, NO FK (Option B), handy for "order again"
    name          NVARCHAR(100)  NOT NULL,
    unit_price    DECIMAL(10,3)  NOT NULL CHECK (unit_price >= 0),   -- item price at checkout, without options
    quantity      INT            NOT NULL CHECK (quantity > 0),
    note          NVARCHAR(255)  NULL,
    line_total    DECIMAL(10,3)  NOT NULL CHECK (line_total >= 0)    -- (unit_price + options) * quantity
);
```

### order_item_option

```sql
CREATE TABLE order_item_option (
    id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    order_item_id  UNIQUEIDENTIFIER NOT NULL REFERENCES order_item(id) ON DELETE CASCADE,
    group_name     NVARCHAR(50)   NOT NULL,   -- "Sauce"
    option_name    NVARCHAR(50)   NOT NULL,   -- "Ketchup"
    extra_price    DECIMAL(10,3)  NOT NULL CHECK (extra_price >= 0)
);
```

## Business decisions

### Deleting menu items vs. keeping order history

Order history has to stay correct even after a store changes or removes a product (spec section 10: the Chicken Sandwich is gone from the menu, but the old order still shows "Chicken Sandwich, with ketchup, 3.750"). There are two ways to do this:

| | Option A: soft delete + linked snapshots | Option B: hard delete + value snapshots |
|---|---|---|
| Deleting a menu item | Not really deleted, just marked (`deleted_at`) | Row is actually deleted |
| Order lines | Reference `menu_item` / `menu_option` by FK | Copy the values (name, price, chosen options) at checkout, no FK to the menu |
| Old orders | Read the item through the FK | Read their own copied values |
| Downside | Deleted rows stay in the DB forever and every menu query must filter them out | Order lines can't be joined back to the current menu item |

**Chosen: Option B, hard delete with value snapshots.** The snapshot doesn't link to anything, so deleting a menu item, option or store never affects an old order.

What this means for the schema:
- Order snapshot tables must **not** have a cascading FK to `menu_item` or `menu_option`. A cascade would delete history, and a normal FK would block the delete.
- `menu_item` has no `deleted_at` (that was only needed for Option A).
- The order also copies the delivery address and the store name, for the same reason: a saved address can be edited or deleted later, and a store can be deleted.

## Indexes

SQL Server doesn't index foreign keys automatically, so they're added by hand.
(Columns that already lead a UNIQUE or PK are covered: `menu_item_option_group.menu_item_id`, `cart.user_id`, `cart_item_option.cart_item_id`, `driver.user_id`.)

```sql
CREATE INDEX ix_address_user_id                       ON address (user_id);
CREATE INDEX ix_store_owner_id                        ON store (owner_id);
CREATE INDEX ix_section_store_id                      ON section (store_id);
CREATE INDEX ix_menu_item_section_id                  ON menu_item (section_id);
CREATE INDEX ix_option_group_store_id                 ON option_group (store_id);
CREATE INDEX ix_menu_option_option_group_id           ON menu_option (option_group_id);
CREATE INDEX ix_menu_item_option_group_option_group_id ON menu_item_option_group (option_group_id);
CREATE INDEX ix_cart_store_id                         ON cart (store_id);
CREATE INDEX ix_cart_item_cart_id                     ON cart_item (cart_id);
CREATE INDEX ix_cart_item_menu_item_id                ON cart_item (menu_item_id);
CREATE INDEX ix_cart_item_option_menu_option_id       ON cart_item_option (menu_option_id);
CREATE INDEX ix_orders_user_id_created_at             ON orders (user_id, created_at);   -- order history, newest first
CREATE INDEX ix_orders_store_id                       ON orders (store_id);
CREATE INDEX ix_orders_driver_id                      ON orders (driver_id);
CREATE INDEX ix_order_item_order_id                   ON order_item (order_id);
CREATE INDEX ix_order_item_option_order_item_id       ON order_item_option (order_item_id);
```

## Backend notes

Things the schema can't do by itself, so the backend has to handle them:

- `updated_at` doesn't update on its own (SQL Server has no `ON UPDATE`). Set it in EF Core's `SaveChanges`.
- Before deleting an address, set `users.last_used_address_id` to `NULL` if it points at that address, otherwise the FK blocks the delete. `ON DELETE SET NULL` can't be used here because SQL Server rejects the cascade cycle with `address.user_id`.
- `DEFAULT NEWID()` is a fallback. With EF Core the GUIDs are normally generated in C#.
- Some links can't cascade because SQL Server rejects multiple cascade paths (Postgres would allow them). The backend deletes the children first, in one transaction (or EF Core `DeleteBehavior.ClientCascade`):
  - deleting a store → delete its carts first (`cart.store_id`)
  - deleting a menu option → delete the `cart_item_option` rows that use it (`cart_item_option.menu_option_id`)
  - deleting an option group → delete its `menu_item_option_group` rows and the `cart_item_option` rows for its options
  - verify a full store delete once against the real database: the menu cascades and the join-table links must all clear in one statement
- Place order, in one transaction: validate the cart, copy it into `orders` / `order_item` / `order_item_option`, then delete the cart.
- Checks the schema can't do: no sold-out items, store open and not paused, each option group's min/max respected, one store per cart ("Empty your cart and start a new one?").
- When the status changes, also set the matching `*_at` column. Times are stored in UTC (Azure SQL runs in UTC) and shown in Amman time (`Asia/Amman`).

## Relationships

| Parent | Child | Type | Via |
|---|---|---|---|
| users | address | 1 : N | `address.user_id` (cascade delete) |
| address | users | 0..1 | `users.last_used_address_id` |
| users | store | 1 : N | `store.owner_id` |
| store | section | 1 : N | `section.store_id` |
| section | menu_item | 1 : N | `menu_item.section_id` |
| store | option_group | 1 : N | `option_group.store_id` |
| option_group | menu_option | 1 : N | `menu_option.option_group_id` |
| menu_item | option_group | N : M | `menu_item_option_group` |
| users | cart | 1 : 0..1 | `cart.user_id` (UNIQUE, cascade delete) |
| store | cart | 1 : N | `cart.store_id` |
| cart | cart_item | 1 : N | `cart_item.cart_id` (cascade delete) |
| menu_item | cart_item | 1 : N | `cart_item.menu_item_id` (cascade delete) |
| cart_item | menu_option | N : M | `cart_item_option` |
| users | driver | 1 : 0..1 | `driver.user_id` (shared PK) |
| users | orders | 1 : N | `orders.user_id` |
| store | orders | 1 : N | `orders.store_id` (set null) |
| driver | orders | 1 : N | `orders.driver_id` (NULL until assigned) |
| orders | order_item | 1 : N | `order_item.order_id` (cascade delete) |
| order_item | order_item_option | 1 : N | `order_item_option.order_item_id` (cascade delete) |

## TODO

- [ ] "Find stores" query: closest first, using the address and store coordinates
