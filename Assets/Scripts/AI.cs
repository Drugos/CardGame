﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    public void MakeTurn()
    {
        StartCoroutine(EnemyTurn(GameManagerScr.Instance.EnemyHandCards));
    }

    IEnumerator EnemyTurn(List<CardController> cards)
    {
        yield return new WaitForSeconds(1);

        int count = cards.Count == 1 ? 1 :
                    Random.Range(0, cards.Count);

        for (int i = 0; i < count; i++)
        {
            if (GameManagerScr.Instance.EnemyFieldCards.Count > 5 ||
                GameManagerScr.Instance.CurrentGame.Enemy.Mana == 0 ||
                GameManagerScr.Instance.EnemyHandCards.Count == 0)
                break;

            List<CardController> cardsList = cards.FindAll(x => GameManagerScr.Instance.CurrentGame.Enemy.Mana >= x.Card.Manacost);

            if (cardsList.Count == 0)
                break;

            if (cardsList[0].Card.IsSpell)
            {
                CastSpell(cardsList[0]);
                yield return new WaitForSeconds(.51f);
            }
            else
            {
                cardsList[0].GetComponent<CardMovementScr>().MoveToField(GameManagerScr.Instance.EnemyField);
                yield return new WaitForSeconds(.51f);
                cardsList[0].transform.SetParent(GameManagerScr.Instance.EnemyField);
                cardsList[0].OnCast();
            }
        }

        yield return new WaitForSeconds(1);

        while (GameManagerScr.Instance.EnemyFieldCards.Exists(x => x.Card.CanAttack))
        {
            var activeCard = GameManagerScr.Instance.EnemyFieldCards.FindAll(x => x.Card.CanAttack)[0];
            bool hasProvocation = GameManagerScr.Instance.PlayerFieldCards.Exists(x => x.Card.IsProvocation);

            if (hasProvocation ||
                Random.Range(0, 2) == 0 &&
                GameManagerScr.Instance.PlayerFieldCards.Count > 0)
            {
                CardController enemy;

                if (hasProvocation)
                    enemy = GameManagerScr.Instance.PlayerFieldCards.Find(x => x.Card.IsProvocation);
                else
                    enemy = GameManagerScr.Instance.PlayerFieldCards[Random.Range(0, GameManagerScr.Instance.PlayerFieldCards.Count)];

                Debug.Log(activeCard.Card.Name + " (" + activeCard.Card.Attack + ";" + activeCard.Card.Defense + ") " + "---> " +
                          enemy.Card.Name + " (" + enemy.Card.Attack + ";" + enemy.Card.Defense + ")");

                activeCard.Movement.MoveToTarget(enemy.transform);
                yield return new WaitForSeconds(.75f);

                GameManagerScr.Instance.CardsFight(activeCard, enemy);
            }
            else
            {
                Debug.Log(activeCard.Card.Name + " (" + activeCard.Card.Attack + ") Attacked hero");

                activeCard.GetComponent<CardMovementScr>().MoveToTarget(GameManagerScr.Instance.PlayerHero.transform);
                yield return new WaitForSeconds(.75f);

                GameManagerScr.Instance.DamageHero(activeCard, false);
            }

            yield return new WaitForSeconds(.2f);
        }

        yield return new WaitForSeconds(1);
        GameManagerScr.Instance.ChangeTurn();
    }

    void CastSpell(CardController card)
    {
        switch (((SpellCard)card.Card).SpellTarget)
        {
            case SpellCard.TargetType.NO_TARGET:

                switch (((SpellCard)card.Card).Spell)
                {
                    case SpellCard.SpellType.HEAL_ALLY_FIELD_CARDS:

                        if (GameManagerScr.Instance.EnemyFieldCards.Count > 0)
                            StartCoroutine(CastCard(card));

                        break;

                    case SpellCard.SpellType.DAMAGE_ENEMY_FIELD_CARDS:

                        if (GameManagerScr.Instance.PlayerFieldCards.Count > 0)
                            StartCoroutine(CastCard(card));

                        break;

                    case SpellCard.SpellType.HEAL_ALLY_HERO:
                        StartCoroutine(CastCard(card));
                        break;

                    case SpellCard.SpellType.DAMAGE_ENEMY_HERO:
                        StartCoroutine(CastCard(card));
                        break;
                }

                break;

            case SpellCard.TargetType.ALLY_CARD_TARGET:

                if (GameManagerScr.Instance.EnemyFieldCards.Count > 0)
                    StartCoroutine(CastCard(card,
                        GameManagerScr.Instance.EnemyFieldCards[Random.Range(0, GameManagerScr.Instance.EnemyFieldCards.Count)]));

                break;

            case SpellCard.TargetType.ENEMY_CARD_TARGET:

                if (GameManagerScr.Instance.PlayerFieldCards.Count > 0)
                    StartCoroutine(CastCard(card,
                        GameManagerScr.Instance.PlayerFieldCards[Random.Range(0, GameManagerScr.Instance.PlayerFieldCards.Count)]));

                break;
        }
    }

    IEnumerator CastCard(CardController spell, CardController target = null)
    {
        if (((SpellCard)spell.Card).SpellTarget == SpellCard.TargetType.NO_TARGET)
        {
            spell.GetComponent<CardMovementScr>().MoveToField(GameManagerScr.Instance.EnemyField);
            yield return new WaitForSeconds(.51f);

            spell.OnCast();
        }
        else
        {
            spell.Info.ShowCardInfo();
            spell.GetComponent<CardMovementScr>().MoveToTarget(target.transform);
            yield return new WaitForSeconds(.51f);

            GameManagerScr.Instance.EnemyHandCards.Remove(spell);
            GameManagerScr.Instance.EnemyFieldCards.Add(spell);
            GameManagerScr.Instance.ReduceMana(false, spell.Card.Manacost);

            spell.Card.IsPlaced = true;

            spell.UseSpell(target);
        }

        string targetStr = target == null ? "no_target" : target.Card.Name;
        Debug.Log("AI spell cast: " + (spell.Card).Name + " target: " + targetStr);
    }
}