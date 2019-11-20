: chars ;
: align ;
: aligned ;

: [char] char , ; immediate
: s>d dup 0< ;
: begin here ; immediate
: nip swap drop ;
: if postpone 0branch here 0 , ; immediate
: min 2dup < if drop else nip then ;
: max 2dup > if drop else nip then ;
: ?dup dup if dup then ;
: case 0 ; immediate
: endcase postpone drop begin ?dup while postpone then repeat ; immediate
: of postpone (of) here 0 , ; immediate
: endof postpone else ; immediate
: value create , does> @ ;
: 0<> 0= 0= ;
: 0> dup 0< 0= swap 0<> and ;
: <> = 0= ;
: buffer: create allot ;
: hex $10 base ! ;
: decimal #10 base ! ;
: true -1 ;
: false 0 ;
: . s>d swap over dabs <# #s rot sign #> type space ;
: u. 0 <# #s #> type space ;
: compile, , ;
: save-input >in @ 1 ;
: restore-input drop >in ! 0 ;
: .s ." <" depth s>d swap over dabs <# #s rot sign #> type ." > "
depth 1+ 1 ?do depth i - pick . loop cr ;
: .r ( n1 n2 -- )
swap s>d swap over dabs <# #s rot sign #>
rot over - spaces type space ;
: u.r ( u n -- )
swap 0 <# #s #> rot over - spaces type space ;
: pad here $100 + ;
: erase 0 fill ;
: 2over 3 pick 3 pick ;
: 2swap >r rot rot r> rot rot ;
: [ 0 state ! ; immediate
: ] -1 state ! ;
: count dup 1+ swap @ ;
: /string dup >r - swap r> + swap ;
: abort depth 0 do drop loop quit ;
: \ refill 0= if source nip >in ! then ; immediate
: bl $20 ;

( from FIG UK )
: /mod >r s>d r> fm/mod ;
: / /mod nip ;
: mod /mod drop ;
: */mod >r m* r> fm/mod ;
: */ */mod nip ;
: ?negate 0< if negate then ;
: sm/rem 2dup xor >r over >r abs >r dabs r> um/mod swap r> ?negate swap r> ?negate ;

( from forth-standard.org )
: isspace? BL 1+ U< ;
: isnotspace? isspace? 0= ;
: xt-skip >R BEGIN DUP WHILE OVER C@ R@ EXECUTE WHILE 1 /STRING REPEAT THEN R> DROP ;
: parse-name SOURCE >IN @ /STRING ['] isspace? xt-skip OVER >R ['] isnotspace? xt-skip 2DUP 1 MIN + SOURCE DROP - >IN ! DROP R> TUCK - ;
: DEFER CREATE ['] ABORT , DOES> @ EXECUTE ;
: defer! >body ! ;
: defer@ >body @ ;
: ACTION-OF STATE @ IF POSTPONE ['] POSTPONE DEFER@ ELSE ' DEFER@ THEN ; IMMEDIATE
: IS STATE @ IF POSTPONE ['] POSTPONE DEFER! ELSE ' DEFER! THEN ; IMMEDIATE
: HOLDS BEGIN DUP WHILE 1- 2DUP + C@ HOLD REPEAT 2DROP ;

\ ----- C64 primitives below

:code c@
	lda LSB,x
	sta + + 1
	lda MSB,x
	sta + + 2
+	lda $cafe
	sta LSB,x
	lda #0
	sta MSB,x
	rts
;code

:code c!
	lda LSB,x
	sta + + 1
	lda MSB,x
	sta + + 2
	lda LSB+1,x
+	sta $1234
	inx
	inx
	rts
;code

:code 1+
	inc LSB, x
	bne +
	inc MSB, x
+	rts
;code

:code litc
	dex

	; load IP
	pla
	sta W
	pla
	sta W + 1

	inc W
	bne +
	inc W + 1
+
	; copy literal to stack
	ldy #0
	lda (W), y
	sta LSB, x
	sty MSB, x

	inc W
	bne +
	inc W + 1
+	jmp (W)
;code

:code lit
	dex

	; load IP
	pla
	sta W
	pla
	sta W + 1

	; copy literal to stack
	ldy #1
	lda (W), y
	sta LSB, x
	iny
	lda (W), y
	sta MSB, x

	lda W
	clc
	adc #3
	sta + + 1
	lda W + 1
	adc #0
	sta + + 2
+	jmp $1234
;code

:code (loop)
	stx	W	; x = stack pointer
	tsx

	inc	$103,x	; i++
	bne	+
	inc	$104,x
+
	lda	$103,x	; lsb check
	cmp	$105,x
	beq	.check_msb

.continue_loop
	ldx	W	; restore x
	jmp	%branch%

.check_msb
	lda	$104,x
	cmp	$106,x
	bne	.continue_loop

	pla		; loop done - skip branch address
	clc
	adc	#3
	sta	W2

	pla
	adc	#0
	sta	W2 + 1

	txa		; sp += 6
	clc
	adc	#6
	tax
	txs

	ldx	W	; restore x
	jmp	(W2)
;code

:code 0branch
	inx
	lda LSB-1, x
	ora MSB-1, x
	beq %branch%

	; skip offset
	pla
	clc
	adc #2
	bcc +
	tay
	pla
	adc #0
	pha
	tya
+	pha
	rts
;code

:code !
	lda LSB, x
	sta W
	lda MSB, x
	sta W + 1

	ldy #0
	lda LSB+1, x
	sta (W), y
	iny
	lda MSB+1, x
	sta (W), y

	inx
	inx
	rts
;code

:code negate
	jsr %invert%
	jmp %1+%
;code

:code 0<
	lda	MSB,x
	and	#$80
	beq	+
	lda	#$ff
+	sta	LSB,x
	sta	MSB,x
	rts
;code

:code dup
	dex
	lda MSB + 1, x
	sta MSB, x
	lda LSB + 1, x
	sta LSB, x
	rts
;code

:code type
-	lda LSB,x
	ora MSB,x
	bne +
	inx
	inx
	rts
+	jsr %over%
	jsr %c@%
	jsr %emit%
	jsr %1%
	jsr %/string%
	jmp -
;code

:code depth
	txa
	eor #$ff
	tay
	iny
	dex
	sty LSB,x
	lda #0
	sta MSB,x
	rts
;code

:code @
	lda LSB,x
	sta W
	lda MSB,x
	sta W+1

	ldy #0
	lda (W),y
	sta LSB,x
	iny
	lda (W),y
	sta MSB,x
	rts
;code

:code =
	ldy #0
	lda LSB, x
	cmp LSB + 1, x
	bne +
	lda MSB, x
	cmp MSB + 1, x
	bne +
	dey
+	inx
	sty MSB, x
	sty LSB, x
	rts
;code

:code (do)
	pla
	sta	W
	pla
	tay

	lda	MSB+1,x
	pha
	lda	LSB+1,x
	pha

	lda	MSB,x
	pha
	lda	LSB,x
	pha

	inx
	inx

	tya
	pha
	lda	W
	pha
	rts
;code

:code i
	jmp %r@%
;code

:code +
	lda LSB, x
	clc
	adc LSB + 1, x
	sta LSB + 1, x

	lda MSB, x
	adc MSB + 1, x
	sta MSB + 1, x

	inx
	rts
;code

:code 0=
	ldy #0
	lda MSB, x
	bne +
	lda LSB, x
	bne +
	dey
+	sty MSB, x
	sty LSB, x
	rts
;code

:code sliteral
	jsr	%r>%
	jsr	%1+%
	jsr	%dup%
	jsr	%1+%
	jsr	%swap%
	jsr	%c@%
	jsr	%2dup%
	jsr	%+%
	jsr	%1-%
	jsr	%>r%
	rts
;code

:code 1-
	lda LSB, x
	bne +
	dec MSB, x
+	dec LSB, x
	rts
;code

:code 2dup
	jsr %over%
	jmp %over%
;code

:code over
	dex
	lda MSB + 2, x
	sta MSB, x
	lda LSB + 2, x
	sta LSB, x
	rts
;code

:code swap
	ldy MSB, x
	lda MSB + 1, x
	sta MSB, x
	sty MSB + 1, x

	ldy LSB, x
	lda LSB + 1, x
	sta LSB, x
	sty LSB + 1, x
	rts
;code

:code 2+
	jsr	%1+%
	jmp	%1+%
;code

:code cr
	jsr	%litc%
	!byte	$d
	jmp	%emit%
;code

:code emit
	lda	LSB, x
	inx
	jmp	PUTCHR
;code

:code /string
	jsr %dup%
	jsr %>r%
	jsr %-%
	jsr %swap%
	jsr %r>%
	jsr %+%
	jmp %swap%
;code

:code -
	lda LSB + 1, x
	sec
	sbc LSB, x
	sta LSB + 1, x
	lda MSB + 1, x
	sbc MSB, x
	sta MSB + 1, x
	inx
	rts
;code

:code 1
	lda	#1
	ldy	#0
	jmp	%pushya%
;code

:code pushya
	dex
	sta	LSB, x
	sty	MSB, x
	rts
;code

:code invert
	lda MSB, x
	eor #$ff
	sta MSB, x
	lda LSB, x
	eor #$ff
	sta LSB,x
	rts
;code

:code branch
	pla
	sta W
	pla
	sta W + 1

	ldy #2
	lda (W), y
	sta + + 2
	dey
	lda (W), y
	sta + + 1
+	jmp $1234
;code

:code	dabs
	jsr	%dup%
	jmp	%?dnegate%
;code

:code	?dnegate
	jsr	%0<%
	inx
	lda	MSB-1,x
	beq	+
	jsr	%dnegate%
+	rts
;code

:code	dnegate
	jsr	%invert%
	jsr	%>r%
	jsr	%invert%
	jsr	%r>%
	jsr	%1%
	jmp	%m+%
;code

:code	m+
	ldy #0
	lda MSB,x
	bpl +
	dey
+	clc
	lda LSB,x
	adc LSB+2,x
	sta LSB+2,x
	lda MSB,x
	adc MSB+2,x
	sta MSB+2,x
	tya
	adc LSB+1,x
	sta LSB+1,x
	tya
	adc MSB+1,x
	sta MSB+1,x
	inx
	rts
;code

:code	+!
	lda	LSB,x
	sta	W
	lda	MSB,x
	sta	W+1

	ldy	#0
	clc

	lda	(W),y
	adc	LSB+1,x
	sta	(W),y
	iny
	lda	(W),y
	adc	MSB+1,x
	sta	(W),y
	inx
	inx
	rts
;code

:code	cells
	jmp	%2*%
;code

:code	2*
	asl	LSB, x
	rol	MSB, x
	rts
;code

:code	2/
	lda	MSB,x
	cmp	#$80
	ror	MSB,x
	ror	LSB,x
;code

:code	and
	lda	MSB, x
	and	MSB + 1, x
	sta	MSB + 1, x

	lda	LSB, x
	and	LSB + 1, x
	sta	LSB + 1, x

	inx
	rts
;code

:code	r>	; must be called using jsr
	pla
	sta W
	pla
	sta W+1
	inc W
	bne +
	inc W+1
+
	dex
	pla
	sta LSB,x
	pla
	sta MSB,x
	jmp (W)
;code

:code	r@	; must be called using jsr
	txa
	tsx
	ldy $103,x
	sty W
	ldy $104,x
	tax
	dex
	sty MSB,x
	lda W
	sta LSB,x
	rts
;code

:code	>r	; must be called using jsr
	pla
	sta W
	pla
	sta W+1
	inc W
	bne +
	inc W+1
+
	lda MSB,x
	pha
	lda LSB,x
	pha
	inx
	jmp (W)
;code

:code	or
	lda	MSB,x
	ora	MSB+1,x
	sta	MSB+1,x
	lda	LSB,x
	ora	LSB+1,x
	sta	LSB+1,x
	inx
	rts
;code

:code	xor
	lda	MSB,x
	eor	MSB+1,x
	sta	MSB+1,x
	lda	LSB,x
	eor	LSB+1,x
	sta	LSB+1,x
	inx
	rts
;code

:code	lshift
-	dec	LSB,x
	bmi	+
	asl	LSB+1,x
	rol	MSB+1,x
	jmp	-
+	inx
	rts
;code

:code	rshift
-	dec	LSB,x
	bmi	+
	lsr	LSB+1,x
	ror	MSB+1,x
	jmp	-
+	inx
	rts
;code