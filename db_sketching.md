# MyThings — Database Sketch

Schema sketch for the customer journey (B05), sections 1–4: users, addresses, stores and the menu.
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
    store_id    UNIQUEIDENTIFIER NOT NULL REFERENCES store(id) on delete cascade,
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
    section_id   UNIQUEIDENTIFIER NOT NULL REFERENCES section(id) on delete cascade,
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
    store_id    UNIQUEIDENTIFIER NOT NULL REFERENCES store(id) on delete cascade,
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
    option_group_id  UNIQUEIDENTIFIER NOT NULL REFERENCES option_group(id),
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
    menu_item_id     UNIQUEIDENTIFIER NOT NULL REFERENCES menu_item(id),
    option_group_id  UNIQUEIDENTIFIER NOT NULL REFERENCES option_group(id),
    UNIQUE (menu_item_id, option_group_id)
);
```

CREATE TABLE cart (
    id uuid pk 
    user_id not null refrence user on id on delete cascade
    store_id not null refrence store on id -- backend should delete carts for the store when store is deleted 
    created_at
    updated_at
);

CREATE TABLE cart_item (
    id uuid pk
    cart_id not null refrences cart on id on delete cascade
    menu_item_id not null refrences menu_item on id on delete cascade
    quantity int not null check (quantity > 0 )
    created_at
    updated_at
);

-- tbh on cart_item i think i can make a json object on a column that have a lsit of uuids of the option it has without cart_item_options

CREATE TABLE cart_item_option (
    id uuid pk
    cart_item_id not null refrence cart_item on id on delete cascade
    menu_option_id ont null refrence menu_options on id -- backend should delete on menu_option delete 
    unique(cart_item_id, menu_option_id)
)

CREATE TABLE orders ( --order is reserved
    id uuid pk
    cart_id not null refrence cart on id
    driver_id uuid refrence driver on user_id
    note text
    cancelation_note text not null 
    payment varchar check (payment in (card, cash)) not null
    total DECIMAL(10,3) not null 
    status varchar not null check in (status IN ('placed', 'accepted', 'on the way', 'delivered', 'declined', cancelled))
    created_at
    updated_at
);

CREATE TABLE driver (
    users_id not null uuid pk refrence user on id
    is_active boolen default false not null
    created_at
    updated_at
);






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
- `menu_item.deleted_at` is only needed for Option A.

## Indexes

SQL Server doesn't index foreign keys automatically, so they're added by hand.
(`menu_item_option_group.menu_item_id` is already covered by the UNIQUE constraint.)

```sql
CREATE INDEX ix_address_user_id                       ON address (user_id);
CREATE INDEX ix_store_owner_id                        ON store (owner_id);
CREATE INDEX ix_section_store_id                      ON section (store_id);
CREATE INDEX ix_menu_item_section_id                  ON menu_item (section_id);
CREATE INDEX ix_option_group_store_id                 ON option_group (store_id);
CREATE INDEX ix_menu_option_option_group_id           ON menu_option (option_group_id);
CREATE INDEX ix_menu_item_option_group_option_group_id ON menu_item_option_group (option_group_id);
```

## Backend notes

Things the schema can't do by itself, so the backend has to handle them:

- `updated_at` doesn't update on its own (SQL Server has no `ON UPDATE`). Set it in EF Core's `SaveChanges`.
- Before deleting an address, set `users.last_used_address_id` to `NULL` if it points at that address, otherwise the FK blocks the delete. `ON DELETE SET NULL` can't be used here because SQL Server rejects the cascade cycle with `address.user_id`.
- `DEFAULT NEWID()` is a fallback. With EF Core the GUIDs are normally generated in C#.

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

## TODO

- [ ] "Find stores" query: closest first, using the address and store coordinates
